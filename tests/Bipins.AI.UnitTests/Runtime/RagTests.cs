using Bipins.AI.Core.Ingestion;
using Bipins.AI.Core.Models;
using Bipins.AI.Core.Rag;
using Bipins.AI.Vector;
using Bipins.AI.Runtime.Rag;
using Bipins.AI.Runtime.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Runtime;

public class RagTests
{
    private readonly Mock<ILogger<DefaultRagComposer>> _composerLogger;
    private readonly Mock<ILogger<VectorRetriever>> _retrieverLogger;
    private readonly DefaultRagComposer _composer;

    public RagTests()
    {
        _composerLogger = new Mock<ILogger<DefaultRagComposer>>();
        _retrieverLogger = new Mock<ILogger<VectorRetriever>>();
        _composer = new DefaultRagComposer(_composerLogger.Object);
    }

    [Fact]
    public void DefaultRagComposer_Compose_WithChunks_AddsContext()
    {
        var originalRequest = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "What is AI?")
        });

        var chunks = new[]
        {
            new RagChunk(
                new Chunk("chunk1", "AI is artificial intelligence", 0, 30),
                0.95f,
                "uri1",
                "doc1"),
            new RagChunk(
                new Chunk("chunk2", "Machine learning is a subset of AI", 0, 35),
                0.90f,
                "uri2",
                "doc2")
        };

        var retrieved = new RetrieveResult(
            chunks,
            new ReadOnlyMemory<float>(new float[] { 0.1f }),
            2);

        var result = _composer.Compose(originalRequest, retrieved);

        Assert.NotEqual(originalRequest, result);
        Assert.Contains(result.Messages, m => m.Role == MessageRole.System);
        var systemMessage = result.Messages.First(m => m.Role == MessageRole.System);
        Assert.Contains("AI is artificial intelligence", systemMessage.Content);
        Assert.Contains("Source 1", systemMessage.Content);
        Assert.Contains("Source 2", systemMessage.Content);
    }

    [Fact]
    public void DefaultRagComposer_Compose_WithNoChunks_ReturnsOriginal()
    {
        var originalRequest = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "What is AI?")
        });

        var retrieved = new RetrieveResult(
            Array.Empty<RagChunk>(),
            new ReadOnlyMemory<float>(new float[] { 0.1f }),
            0);

        var result = _composer.Compose(originalRequest, retrieved);

        Assert.Equal(originalRequest, result);
    }

    [Fact]
    public void DefaultRagComposer_Compose_WithExistingSystemMessage_PrependsContext()
    {
        var originalRequest = new ChatRequest(new[]
        {
            new Message(MessageRole.System, "You are a helpful assistant."),
            new Message(MessageRole.User, "What is AI?")
        });

        var chunks = new[]
        {
            new RagChunk(
                new Chunk("chunk1", "AI is artificial intelligence", 0, 30),
                0.95f)
        };

        var retrieved = new RetrieveResult(
            chunks,
            new ReadOnlyMemory<float>(new float[] { 0.1f }),
            1);

        var result = _composer.Compose(originalRequest, retrieved);

        var systemMessage = result.Messages.First(m => m.Role == MessageRole.System);
        Assert.Contains("AI is artificial intelligence", systemMessage.Content);
        Assert.Contains("You are a helpful assistant.", systemMessage.Content);
    }

    [Fact]
    public void DefaultRagComposer_Compose_IncludesSourceUri()
    {
        var originalRequest = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "What is AI?")
        });

        var chunks = new[]
        {
            new RagChunk(
                new Chunk("chunk1", "AI is artificial intelligence", 0, 30),
                0.95f,
                "https://example.com/doc1",
                "doc1")
        };

        var retrieved = new RetrieveResult(
            chunks,
            new ReadOnlyMemory<float>(new float[] { 0.1f }),
            1);

        var result = _composer.Compose(originalRequest, retrieved);

        var systemMessage = result.Messages.First(m => m.Role == MessageRole.System);
        Assert.Contains("https://example.com/doc1", systemMessage.Content);
        Assert.Contains("Document: doc1", systemMessage.Content);
    }

    [Fact]
    public async Task VectorRetriever_RetrieveAsync_WithValidRequest_ReturnsResults()
    {
        var router = new Mock<IModelRouter>();
        var vectorStore = new Mock<IVectorStore>();
        var embeddingModel = new Mock<IEmbeddingModel>();
        var retriever = new VectorRetriever(_retrieverLogger.Object, router.Object, vectorStore.Object);

        var queryVector = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f });
        var embeddingResponse = new EmbeddingResponse(
            new[] { queryVector },
            new Usage(10, 0, 10),
            "text-embedding-ada-002");

        var vectorRecord = new VectorRecord(
            "id1",
            queryVector,
            "AI is artificial intelligence",
            new Dictionary<string, object> { { "key", "value" } },
            "uri1",
            "doc1",
            "chunk1",
            "tenant1");

        var vectorResponse = new VectorQueryResponse(
            new[] { new VectorMatch(vectorRecord, 0.95f) });

        router.Setup(r => r.SelectEmbeddingModelAsync(
                "tenant1",
                It.IsAny<EmbeddingRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingModel.Object);

        embeddingModel.Setup(m => m.EmbedAsync(
                It.IsAny<EmbeddingRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingResponse);

        vectorStore.Setup(v => v.QueryAsync(
                It.Is<VectorQueryRequest>(req => req.TenantId == "tenant1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(vectorResponse);

        var request = new RetrieveRequest("What is AI?", "tenant1", TopK: 5);
        var result = await retriever.RetrieveAsync(request);

        Assert.NotNull(result);
        Assert.Single(result.Chunks);
        Assert.Equal(0.95f, result.Chunks[0].Score);
        Assert.Equal("AI is artificial intelligence", result.Chunks[0].Chunk.Text);
    }

    [Fact]
    public async Task VectorRetriever_RetrieveAsync_WithUserFilter_CombinesWithTenantFilter()
    {
        var router = new Mock<IModelRouter>();
        var vectorStore = new Mock<IVectorStore>();
        var embeddingModel = new Mock<IEmbeddingModel>();
        var retriever = new VectorRetriever(_retrieverLogger.Object, router.Object, vectorStore.Object);

        var queryVector = new ReadOnlyMemory<float>(new float[] { 0.1f });
        var embeddingResponse = new EmbeddingResponse(
            new[] { queryVector },
            null,
            "text-embedding-ada-002");

        var userFilter = new VectorFilterPredicate(new FilterPredicate("category", FilterOperator.Eq, "tech"));
        var vectorResponse = new VectorQueryResponse(Array.Empty<VectorMatch>());

        router.Setup(r => r.SelectEmbeddingModelAsync(
                It.IsAny<string>(),
                It.IsAny<EmbeddingRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingModel.Object);

        embeddingModel.Setup(m => m.EmbedAsync(
                It.IsAny<EmbeddingRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingResponse);

        VectorQueryRequest? capturedRequest = null;
        vectorStore.Setup(v => v.QueryAsync(
                It.IsAny<VectorQueryRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<VectorQueryRequest, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(vectorResponse);

        var request = new RetrieveRequest("query", "tenant1", TopK: 5, Filter: userFilter);
        await retriever.RetrieveAsync(request);

        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequest.Filter);
        var andFilter = capturedRequest.Filter as VectorFilterAnd;
        Assert.NotNull(andFilter);
        Assert.Equal(2, andFilter.Filters.Count);
    }

    [Fact]
    public async Task VectorRetriever_RetrieveAsync_WithEmptyTenantId_ThrowsException()
    {
        var router = new Mock<IModelRouter>();
        var vectorStore = new Mock<IVectorStore>();
        var retriever = new VectorRetriever(_retrieverLogger.Object, router.Object, vectorStore.Object);

        var request = new RetrieveRequest("query", "", TopK: 5);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await retriever.RetrieveAsync(request));
    }

    [Fact]
    public async Task VectorRetriever_RetrieveAsync_WithNoEmbedding_ThrowsException()
    {
        var router = new Mock<IModelRouter>();
        var vectorStore = new Mock<IVectorStore>();
        var embeddingModel = new Mock<IEmbeddingModel>();
        var retriever = new VectorRetriever(_retrieverLogger.Object, router.Object, vectorStore.Object);

        var emptyResponse = new EmbeddingResponse(
            Array.Empty<ReadOnlyMemory<float>>(),
            null,
            "text-embedding-ada-002");

        router.Setup(r => r.SelectEmbeddingModelAsync(
                It.IsAny<string>(),
                It.IsAny<EmbeddingRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingModel.Object);

        embeddingModel.Setup(m => m.EmbedAsync(
                It.IsAny<EmbeddingRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyResponse);

        var request = new RetrieveRequest("query", "tenant1", TopK: 5);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await retriever.RetrieveAsync(request));
    }

    [Fact]
    public async Task VectorRetriever_RetrieveAsync_WithEmptyResults_ReturnsEmpty()
    {
        var router = new Mock<IModelRouter>();
        var vectorStore = new Mock<IVectorStore>();
        var embeddingModel = new Mock<IEmbeddingModel>();
        var retriever = new VectorRetriever(_retrieverLogger.Object, router.Object, vectorStore.Object);

        var queryVector = new ReadOnlyMemory<float>(new float[] { 0.1f });
        var embeddingResponse = new EmbeddingResponse(
            new[] { queryVector },
            null,
            "text-embedding-ada-002");

        var vectorResponse = new VectorQueryResponse(Array.Empty<VectorMatch>());

        router.Setup(r => r.SelectEmbeddingModelAsync(
                It.IsAny<string>(),
                It.IsAny<EmbeddingRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingModel.Object);

        embeddingModel.Setup(m => m.EmbedAsync(
                It.IsAny<EmbeddingRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingResponse);

        vectorStore.Setup(v => v.QueryAsync(
                It.IsAny<VectorQueryRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(vectorResponse);

        var request = new RetrieveRequest("query", "tenant1", TopK: 5);
        var result = await retriever.RetrieveAsync(request);

        Assert.NotNull(result);
        Assert.Empty(result.Chunks);
        Assert.Equal(0, result.TotalMatches);
    }
}
