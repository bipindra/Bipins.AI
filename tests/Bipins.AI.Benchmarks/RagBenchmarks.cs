using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Bipins.AI.Core.Ingestion;
using Bipins.AI.Core.Models;
using Bipins.AI.Core.Rag;
using Bipins.AI.Vector;
using Bipins.AI.Runtime.Rag;
using Bipins.AI.Runtime.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Bipins.AI.Benchmarks;

/// <summary>
/// Benchmarks for RAG (Retrieval-Augmented Generation) operations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class RagBenchmarks
{
    private readonly IRetriever _retriever;
    private readonly IRagComposer _composer;
    private readonly Mock<IModelRouter> _routerMock;
    private readonly Mock<IVectorStore> _vectorStoreMock;
    private readonly string _tenantId = "benchmark-tenant";

    public RagBenchmarks()
    {
        _routerMock = new Mock<IModelRouter>();
        _vectorStoreMock = new Mock<IVectorStore>();
        
        // Setup mock embedding model
        var embeddingModelMock = new Mock<IEmbeddingModel>();
        embeddingModelMock
            .Setup(m => m.EmbedAsync(It.IsAny<EmbeddingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmbeddingRequest req, CancellationToken ct) =>
            {
                var vectors = req.Inputs.Select(t => (ReadOnlyMemory<float>)new float[1536].AsMemory()).ToList();
                return new EmbeddingResponse(vectors, null, "text-embedding-ada-002");
            });

        _routerMock
            .Setup(r => r.SelectEmbeddingModelAsync(It.IsAny<string>(), It.IsAny<EmbeddingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingModelMock.Object);

        // Setup mock vector store
        _vectorStoreMock
            .Setup(v => v.QueryAsync(It.IsAny<VectorQueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VectorQueryRequest req, CancellationToken ct) =>
            {
                var matches = Enumerable.Range(0, Math.Min(req.TopK, 5))
                    .Select(i => new VectorMatch(
                        new VectorRecord(
                            $"chunk_{i}",
                            new float[1536].AsMemory(),
                            $"Chunk {i} text content",
                            new Dictionary<string, object> { ["docId"] = $"doc_{i}" },
                            $"doc_{i}",
                            $"doc_{i}",
                            $"chunk_{i}",
                            req.TenantId),
                        0.9f - (i * 0.1f)))
                    .ToList();
                return new VectorQueryResponse(matches);
            });

        _retriever = new VectorRetriever(
            NullLogger<VectorRetriever>.Instance,
            _routerMock.Object,
            _vectorStoreMock.Object);

        _composer = new DefaultRagComposer(NullLogger<DefaultRagComposer>.Instance);
    }

    [Benchmark]
    [Arguments(5)]
    [Arguments(10)]
    [Arguments(20)]
    public async Task RetrieveChunks(int topK)
    {
        var request = new RetrieveRequest(
            "What is machine learning?",
            _tenantId,
            TopK: topK);

        var result = await _retriever.RetrieveAsync(request);
    }

    [Benchmark]
    public async Task ComposeRagRequest()
    {
        var chatRequest = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "What is machine learning?")
        });

        var retrieveResult = new RetrieveResult(
            new List<RagChunk>
            {
                new RagChunk(
                    new Chunk("chunk1", "Machine learning is...", 0, 20),
                    0.95f,
                    "doc1",
                    "doc1")
            },
            new float[1536].AsMemory(),
            1);

        var augmentedRequest = _composer.Compose(chatRequest, retrieveResult);
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    public async Task FullRagPipeline(int numChunks)
    {
        // Retrieve
        var retrieveRequest = new RetrieveRequest(
            "What is machine learning?",
            _tenantId,
            TopK: numChunks);

        var retrieved = await _retriever.RetrieveAsync(retrieveRequest);

        // Compose
        var chatRequest = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "What is machine learning?")
        });

        var augmentedRequest = _composer.Compose(chatRequest, retrieved);
    }
}
