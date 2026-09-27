using Bipins.AI.Core.Ingestion;
using Bipins.AI.Core.Models;
using Bipins.AI.Vector;
using Bipins.AI.Runtime.Routing;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Ingestion;

/// <summary>
/// Default indexer that generates embeddings and stores them in a vector store.
/// </summary>
public class DefaultIndexer : IIndexer
{
    private readonly ILogger<DefaultIndexer> _logger;
    private readonly IModelRouter _router;
    private readonly IVectorStore _vectorStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultIndexer"/> class.
    /// </summary>
    public DefaultIndexer(
        ILogger<DefaultIndexer> logger,
        IModelRouter router,
        IVectorStore vectorStore)
    {
        _logger = logger;
        _router = router;
        _vectorStore = vectorStore;
    }

    /// <inheritdoc />
    public async Task<IndexResult> IndexAsync(IEnumerable<Chunk> chunks, IndexOptions options, CancellationToken cancellationToken = default)
    {
        TenantValidator.ValidateOrThrow(options.TenantId);
        if (options.UpdateMode == UpdateMode.Update && options.DeleteOldVersions && string.IsNullOrWhiteSpace(options.VersionId))
            options = options with { VersionId = Guid.NewGuid().ToString("N") };
        var chunkList = chunks.ToList();
        if (chunkList.Count == 0)
        {
            return new IndexResult(0, 0);
        }

        var errors = new List<string>();
        var vectorsCreated = 0;

        try
        {
            // Get embedding model
            var embeddingModel = await _router.SelectEmbeddingModelAsync(
                options.TenantId,
                new EmbeddingRequest(Array.Empty<string>()),
                cancellationToken);

            // Generate embeddings in batches
            var texts = chunkList.Select(c => c.Text).ToList();
            var embeddingRequest = new EmbeddingRequest(texts);
            var embeddingResponse = await embeddingModel.EmbedAsync(embeddingRequest, cancellationToken);

            if (embeddingResponse.Vectors.Count != chunkList.Count)
            {
                throw new InvalidOperationException(
                    $"Expected {chunkList.Count} embeddings but got {embeddingResponse.Vectors.Count}");
            }

            // Create vector records
            var records = new List<VectorRecord>();
            for (int i = 0; i < chunkList.Count; i++)
            {
                var chunk = chunkList[i];
                var vector = embeddingResponse.Vectors[i];

                var metadata = new Dictionary<string, object>
                {
                    ["text"] = chunk.Text,
                    ["sourceUri"] = options.DocId ?? string.Empty,
                    ["docId"] = options.DocId ?? string.Empty,
                    ["chunkId"] = chunk.Id,
                    ["tenantId"] = options.TenantId,
                    ["createdAt"] = DateTime.UtcNow
                };

                if (options.VersionId != null)
                {
                    metadata["versionId"] = options.VersionId;
                }

                // Merge chunk metadata
                if (chunk.Metadata != null)
                {
                    foreach (var kvp in chunk.Metadata)
                    {
                        // Caller metadata cannot override isolation or version ownership.
                        if (kvp.Key != "tenantId" && kvp.Key != "docId" && kvp.Key != "versionId" && kvp.Key != "chunkId")
                            metadata[kvp.Key] = kvp.Value;
                    }
                }

                // A replacement must not overwrite existing IDs before its write completes.
                var record = new VectorRecord(
                    options.UpdateMode == UpdateMode.Update ? Guid.NewGuid().ToString() : chunk.Id,
                    vector,
                    chunk.Text,
                    metadata,
                    options.DocId,
                    options.DocId,
                    chunk.Id,
                    options.TenantId,
                    options.VersionId);

                records.Add(record);
            }

            // Commit the replacement first. A failed write must leave old versions intact.
            await _vectorStore.UpsertAsync(new VectorUpsertRequest(records, options.CollectionName), cancellationToken);
            vectorsCreated = records.Count;

            if (options.UpdateMode == UpdateMode.Update && options.DocId != null && options.DeleteOldVersions)
            {
                // Use the actual embedding dimension and delete bounded pages until exhausted.
                var filter = new VectorFilterAnd(new VectorFilter[]
                {
                    new VectorFilterPredicate(new FilterPredicate("tenantId", FilterOperator.Eq, options.TenantId)),
                    new VectorFilterPredicate(new FilterPredicate("docId", FilterOperator.Eq, options.DocId)),
                    new VectorFilterPredicate(new FilterPredicate("versionId", FilterOperator.Ne, options.VersionId!))
                });
                var deletedIds = new HashSet<string>();
                var newIds = new HashSet<string>(records.Select(r => r.Id));
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var page = await _vectorStore.QueryAsync(new VectorQueryRequest(
                        records[0].Vector, 256, options.TenantId, filter, options.CollectionName), cancellationToken);
                    if (page.Matches.Count == 0) break;
                    var oldIds = page.Matches.Select(m => m.Record.Id).Distinct().ToList();
                    if (oldIds.Any(id => newIds.Contains(id) || deletedIds.Contains(id)))
                        throw new InvalidOperationException("Old-version cleanup did not make progress; replacement records are preserved.");
                    await _vectorStore.DeleteAsync(new VectorDeleteRequest(oldIds, options.CollectionName), cancellationToken);
                    foreach (var id in oldIds) deletedIds.Add(id);
                }
            }

            _logger.LogInformation(
                "Indexed {Count} chunks for tenant {TenantId}, doc {DocId}",
                records.Count,
                options.TenantId,
                options.DocId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing chunks");
            errors.Add(ex.Message);
        }

        return new IndexResult(chunkList.Count, vectorsCreated, errors.Count > 0 ? errors : null);
    }
}
