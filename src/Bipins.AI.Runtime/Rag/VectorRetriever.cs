using Bipins.AI.Core.Ingestion;
using Bipins.AI.Core.Models;
using Bipins.AI.Core.Rag;
using Bipins.AI.Core.Vector;
using Bipins.AI.Runtime.Routing;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Runtime.Rag;

/// <summary>
/// Retrieves relevant chunks using vector search.
/// </summary>
public class VectorRetriever : IRetriever
{
    private readonly ILogger<VectorRetriever> _logger;
    private readonly IModelRouter _router;
    private readonly IVectorStore _vectorStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="VectorRetriever"/> class.
    /// </summary>
    public VectorRetriever(
        ILogger<VectorRetriever> logger,
        IModelRouter router,
        IVectorStore vectorStore)
    {
        _logger = logger;
        _router = router;
        _vectorStore = vectorStore;
    }

    /// <inheritdoc />
    public async Task<RetrieveResult> RetrieveAsync(RetrieveRequest request, CancellationToken cancellationToken = default)
    {
        // Validate tenant ID
        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            throw new ArgumentException("TenantId is required for multi-tenant isolation", nameof(request));
        }

        // Generate embedding for the query
        var embeddingModel = await _router.SelectEmbeddingModelAsync(
            request.TenantId,
            new EmbeddingRequest(new[] { request.Query }),
            cancellationToken);

        var embeddingResponse = await embeddingModel.EmbedAsync(
            new EmbeddingRequest(new[] { request.Query }),
            cancellationToken);

        if (embeddingResponse.Vectors.Count == 0)
        {
            throw new InvalidOperationException("Failed to generate query embedding");
        }

        var queryVector = embeddingResponse.Vectors[0];

        // Combine tenant filter with user filter
        var combinedFilter = CombineTenantFilter(request.TenantId, request.Filter);

        // Query vector store with tenant isolation
        var vectorRequest = new VectorQueryRequest(
            queryVector,
            request.TopK,
            request.TenantId,
            combinedFilter,
            request.CollectionName);

        var vectorResponse = await _vectorStore.QueryAsync(vectorRequest, cancellationToken);

        // Convert to RAG chunks
        var ragChunks = vectorResponse.Matches.Select(m =>
        {
            var chunk = new Chunk(
                m.Record.ChunkId ?? m.Record.Id,
                m.Record.Text,
                0,
                m.Record.Text.Length,
                m.Record.Metadata);

            return new RagChunk(
                chunk,
                m.Score,
                m.Record.SourceUri,
                m.Record.DocId);
        }).ToList();

        _logger.LogInformation(
            "Retrieved {Count} chunks for query (top score: {Score})",
            ragChunks.Count,
            ragChunks.FirstOrDefault()?.Score ?? 0);

        return new RetrieveResult(ragChunks, queryVector, ragChunks.Count);
    }

    /// <summary>
    /// Combines tenant filter with user-provided filter to ensure tenant isolation.
    /// </summary>
    private static VectorFilter? CombineTenantFilter(string tenantId, VectorFilter? userFilter)
    {
        var tenantFilter = new VectorFilterPredicate(
            new FilterPredicate("tenantId", FilterOperator.Eq, tenantId));

        if (userFilter == null)
        {
            return tenantFilter;
        }

        // Combine tenant filter with user filter using AND
        return new VectorFilterAnd(new[] { tenantFilter, userFilter });
    }
}
