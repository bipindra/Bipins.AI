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
        // Generate embedding for the query
        var embeddingModel = await _router.SelectEmbeddingModelAsync(
            "default", // TODO: Get from context
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

        // Query vector store
        var vectorRequest = new VectorQueryRequest(
            queryVector,
            request.TopK,
            request.Filter,
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
}
