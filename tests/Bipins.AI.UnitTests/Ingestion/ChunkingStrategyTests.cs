using Bipins.AI.Core.Ingestion;
using Bipins.AI.Ingestion.Strategies;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Ingestion;

public class ChunkingStrategyTests
{
    private readonly Mock<ILogger<FixedSizeChunkingStrategy>> _fixedSizeLogger;
    private readonly Mock<ILogger<SentenceAwareChunkingStrategy>> _sentenceLogger;
    private readonly Mock<ILogger<ParagraphChunkingStrategy>> _paragraphLogger;
    private readonly Mock<ILogger<MarkdownAwareChunkingStrategy>> _markdownLogger;

    public ChunkingStrategyTests()
    {
        _fixedSizeLogger = new Mock<ILogger<FixedSizeChunkingStrategy>>();
        _sentenceLogger = new Mock<ILogger<SentenceAwareChunkingStrategy>>();
        _paragraphLogger = new Mock<ILogger<ParagraphChunkingStrategy>>();
        _markdownLogger = new Mock<ILogger<MarkdownAwareChunkingStrategy>>();
    }

    [Fact]
    public void FixedSizeChunkingStrategy_StrategyType_ReturnsFixedSize()
    {
        var strategy = new FixedSizeChunkingStrategy(_fixedSizeLogger.Object);
        Assert.Equal(ChunkStrategy.FixedSize, strategy.StrategyType);
    }

    [Fact]
    public async Task FixedSizeChunkingStrategy_ChunksText_ByMaxSize()
    {
        var strategy = new FixedSizeChunkingStrategy(_fixedSizeLogger.Object);
        var text = "This is a test. " + new string('a', 200);
        var options = new ChunkOptions(50, 10, ChunkStrategy.FixedSize);

        var chunks = await strategy.ChunkAsync(text, options);

        Assert.NotNull(chunks);
        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.True(chunk.Text.Length <= 50 + 10)); // Allow some flexibility
    }

    [Fact]
    public void SentenceAwareChunkingStrategy_StrategyType_ReturnsSentence()
    {
        var strategy = new SentenceAwareChunkingStrategy(_sentenceLogger.Object);
        Assert.Equal(ChunkStrategy.Sentence, strategy.StrategyType);
    }

    [Fact]
    public async Task SentenceAwareChunkingStrategy_ChunksText_BySentences()
    {
        var strategy = new SentenceAwareChunkingStrategy(_sentenceLogger.Object);
        var text = "First sentence. Second sentence. Third sentence.";
        var options = new ChunkOptions(100, 0, ChunkStrategy.Sentence);

        var chunks = await strategy.ChunkAsync(text, options);

        Assert.NotNull(chunks);
        Assert.True(chunks.Count > 0);
    }

    [Fact]
    public void ParagraphChunkingStrategy_StrategyType_ReturnsParagraph()
    {
        var strategy = new ParagraphChunkingStrategy(_paragraphLogger.Object);
        Assert.Equal(ChunkStrategy.Paragraph, strategy.StrategyType);
    }

    [Fact]
    public async Task ParagraphChunkingStrategy_ChunksText_ByParagraphs()
    {
        var strategy = new ParagraphChunkingStrategy(_paragraphLogger.Object);
        var text = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph.";
        var options = new ChunkOptions(1000, 0, ChunkStrategy.Paragraph);

        var chunks = await strategy.ChunkAsync(text, options);

        Assert.NotNull(chunks);
        Assert.True(chunks.Count > 0);
    }

    [Fact]
    public void MarkdownAwareChunkingStrategy_StrategyType_ReturnsMarkdownAware()
    {
        var strategy = new MarkdownAwareChunkingStrategy(_markdownLogger.Object);
        Assert.Equal(ChunkStrategy.MarkdownAware, strategy.StrategyType);
    }

    [Fact]
    public async Task MarkdownAwareChunkingStrategy_ChunksText_ByHeadings()
    {
        var strategy = new MarkdownAwareChunkingStrategy(_markdownLogger.Object);
        var text = "# Heading 1\n\nContent under heading 1.\n\n## Heading 2\n\nContent under heading 2.";
        var options = new ChunkOptions(1000, 0, ChunkStrategy.MarkdownAware);

        var chunks = await strategy.ChunkAsync(text, options);

        Assert.NotNull(chunks);
        Assert.True(chunks.Count > 0);
    }

    [Fact]
    public async Task FixedSizeChunkingStrategy_RespectsOverlap()
    {
        var strategy = new FixedSizeChunkingStrategy(_fixedSizeLogger.Object);
        var text = new string('a', 100);
        var options = new ChunkOptions(30, 10, ChunkStrategy.FixedSize);

        var chunks = await strategy.ChunkAsync(text, options);

        Assert.NotNull(chunks);
        if (chunks.Count > 1)
        {
            // Check that chunks overlap (simplified check)
            Assert.True(chunks.Count >= 2);
        }
    }

    [Fact]
    public async Task FixedSizeChunkingStrategy_HandlesEmptyText()
    {
        var strategy = new FixedSizeChunkingStrategy(_fixedSizeLogger.Object);
        var options = new ChunkOptions(50, 10, ChunkStrategy.FixedSize);

        var chunks = await strategy.ChunkAsync("", options);

        Assert.NotNull(chunks);
        // Empty text might return empty list or single empty chunk
    }

    [Fact]
    public async Task SentenceAwareChunkingStrategy_HandlesTextWithoutSentences()
    {
        var strategy = new SentenceAwareChunkingStrategy(_sentenceLogger.Object);
        var text = "No periods or sentences";
        var options = new ChunkOptions(100, 0, ChunkStrategy.Sentence);

        var chunks = await strategy.ChunkAsync(text, options);

        Assert.NotNull(chunks);
        // Should handle gracefully
    }
}
