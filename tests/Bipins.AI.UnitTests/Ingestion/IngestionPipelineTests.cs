using Bipins.AI.Core.Ingestion;
using Bipins.AI.Ingestion;
using Bipins.AI.Ingestion.Strategies;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace Bipins.AI.UnitTests.Ingestion;

public class IngestionPipelineTests
{
    private readonly Mock<ILogger<IngestionPipeline>> _pipelineLogger;
    private readonly Mock<IDocumentLoader> _documentLoader;
    private readonly Mock<ITextExtractor> _textExtractor;
    private readonly Mock<IChunker> _chunker;
    private readonly Mock<IMetadataEnricher> _enricher;
    private readonly Mock<IIndexer> _indexer;

    public IngestionPipelineTests()
    {
        _pipelineLogger = new Mock<ILogger<IngestionPipeline>>();
        _documentLoader = new Mock<IDocumentLoader>();
        _textExtractor = new Mock<ITextExtractor>();
        _chunker = new Mock<IChunker>();
        _enricher = new Mock<IMetadataEnricher>();
        _indexer = new Mock<IIndexer>();
    }

    [Fact]
    public async Task IngestionPipeline_IngestAsync_WithFilePath_LoadsAndProcesses()
    {
        var pipeline = new IngestionPipeline(
            _pipelineLogger.Object,
            _documentLoader.Object,
            _textExtractor.Object,
            _chunker.Object,
            _enricher.Object,
            _indexer.Object);

        var document = new Document("file.txt", Encoding.UTF8.GetBytes("Test content"), "text/plain");
        var chunks = new[]
        {
            new Chunk("chunk1", "Test content", 0, 12)
        };
        var indexResult = new IndexResult(1, 1);

        _documentLoader.Setup(d => d.LoadAsync("file.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _textExtractor.Setup(e => e.ExtractAsync(document, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test content");

        _chunker.Setup(c => c.ChunkAsync("Test content", It.IsAny<ChunkOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);


        _indexer.Setup(i => i.IndexAsync(
                It.IsAny<IEnumerable<Chunk>>(),
                It.IsAny<IndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(indexResult);

        var options = new IndexOptions("tenant1", "doc1");

        var result = await pipeline.IngestAsync("file.txt", options);

        Assert.NotNull(result);
        Assert.Equal(1, result.ChunksIndexed);
        Assert.Equal(1, result.VectorsCreated);
    }

    [Fact]
    public async Task IngestionPipeline_IngestAsync_WithText_ProcessesDirectly()
    {
        var pipeline = new IngestionPipeline(
            _pipelineLogger.Object,
            _documentLoader.Object,
            _textExtractor.Object,
            _chunker.Object,
            _enricher.Object,
            _indexer.Object);

        var chunks = new[]
        {
            new Chunk("chunk1", "Test content", 0, 12)
        };
        var indexResult = new IndexResult(1, 1);

        var document = new Document("text", Encoding.UTF8.GetBytes("Test content"), "text/plain");
        
        _chunker.Setup(c => c.ChunkAsync("Test content", It.IsAny<ChunkOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        _enricher.Setup(e => e.EnrichAsync(It.IsAny<Chunk>(), It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, object>());

        _indexer.Setup(i => i.IndexAsync(
                It.IsAny<IEnumerable<Chunk>>(),
                It.IsAny<IndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(indexResult);

        _documentLoader.Setup(d => d.LoadAsync("file.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _textExtractor.Setup(e => e.ExtractAsync(document, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test content");

        var options = new IndexOptions("tenant1", "doc1");

        var result = await pipeline.IngestAsync("file.txt", options);

        Assert.NotNull(result);
        Assert.Equal(1, result.ChunksIndexed);
    }

    [Fact]
    public async Task IngestionPipeline_IngestAsync_WithError_HandlesGracefully()
    {
        var pipeline = new IngestionPipeline(
            _pipelineLogger.Object,
            _documentLoader.Object,
            _textExtractor.Object,
            _chunker.Object,
            _enricher.Object,
            _indexer.Object);

        _documentLoader.Setup(d => d.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("File not found"));

        var options = new IndexOptions("tenant1", "doc1");

        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await pipeline.IngestAsync("nonexistent.txt", options));
    }

    [Fact]
    public async Task IngestionPipeline_IngestAsync_WithChunkingError_PropagatesException()
    {
        var pipeline = new IngestionPipeline(
            _pipelineLogger.Object,
            _documentLoader.Object,
            _textExtractor.Object,
            _chunker.Object,
            _enricher.Object,
            _indexer.Object);

        var document = new Document("file.txt", Encoding.UTF8.GetBytes("Test content"), "text/plain");

        _documentLoader.Setup(d => d.LoadAsync("file.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _textExtractor.Setup(e => e.ExtractAsync(document, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test content");

        _chunker.Setup(c => c.ChunkAsync(It.IsAny<string>(), It.IsAny<ChunkOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Chunking failed"));

        var options = new IndexOptions("tenant1", "doc1");

        await Assert.ThrowsAsync<Exception>(async () =>
            await pipeline.IngestAsync("file.txt", options));
    }

    [Fact]
    public async Task IngestionPipeline_IngestAsync_EnrichesChunks()
    {
        var pipeline = new IngestionPipeline(
            _pipelineLogger.Object,
            _documentLoader.Object,
            _textExtractor.Object,
            _chunker.Object,
            _enricher.Object,
            _indexer.Object);

        var document = new Document("file.txt", Encoding.UTF8.GetBytes("Test content"), "text/plain");
        var chunks = new[]
        {
            new Chunk("chunk1", "Test content", 0, 12)
        };
        var indexResult = new IndexResult(1, 1);

        _documentLoader.Setup(d => d.LoadAsync("file.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _textExtractor.Setup(e => e.ExtractAsync(document, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test content");

        _chunker.Setup(c => c.ChunkAsync("Test content", It.IsAny<ChunkOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        _enricher.Setup(e => e.EnrichAsync(It.IsAny<Chunk>(), document, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, object> { { "key", "value" } });

        _indexer.Setup(i => i.IndexAsync(
                It.IsAny<IEnumerable<Chunk>>(),
                It.IsAny<IndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(indexResult);

        var options = new IndexOptions("tenant1", "doc1");

        await pipeline.IngestAsync("file.txt", options);

        _enricher.Verify(e => e.EnrichAsync(
            It.IsAny<Chunk>(),
            document,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
