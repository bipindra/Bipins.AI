using Bipins.AI.Core.Ingestion;
using Bipins.AI.Core.Models;
using Bipins.AI.Core.Vector;
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
                    ["tenantId"] = options.TenantId
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
                        metadata[kvp.Key] = kvp.Value;
                    }
                }

                var record = new VectorRecord(
                    chunk.Id,
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

            // Upsert to vector store
            var upsertRequest = new VectorUpsertRequest(records, options.CollectionName);
            await _vectorStore.UpsertAsync(upsertRequest, cancellationToken);

            vectorsCreated = records.Count;
            _logger.LogInformation(
                "Indexed {Count} chunks for tenant {TenantId}, doc {DocId}",
                records.Count,
                options.TenantId,
                options.DocId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing chunks");
            errors.Add(ex.Message);
        }

        return new IndexResult(chunkList.Count, vectorsCreated, errors.Count > 0 ? errors : null);
    }
}
