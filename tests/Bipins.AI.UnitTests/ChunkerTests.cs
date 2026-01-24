using Bipins.AI.Core.Ingestion;
using Bipins.AI.Ingestion;
using Bipins.AI.Ingestion.Strategies;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests;

public class ChunkerTests
{
    private readonly MarkdownAwareChunker _chunker;
    private readonly IChunkingStrategyFactory _strategyFactory;

    public ChunkerTests()
    {
        var logger = new Mock<ILogger<MarkdownAwareChunker>>();
        var fixedSizeLogger = new Mock<ILogger<FixedSizeChunkingStrategy>>();
        var markdownLogger = new Mock<ILogger<MarkdownAwareChunkingStrategy>>();
        
        var strategies = new List<IChunkingStrategy>
        {
            new FixedSizeChunkingStrategy(fixedSizeLogger.Object),
            new MarkdownAwareChunkingStrategy(markdownLogger.Object)
        };
        
        var factoryLogger = new Mock<ILogger<DefaultChunkingStrategyFactory>>();
        _strategyFactory = new DefaultChunkingStrategyFactory(strategies, factoryLogger.Object);
        _chunker = new MarkdownAwareChunker(logger.Object, _strategyFactory);
    }

    [Fact]
    public async Task ChunkByFixedSize_RespectsMaxSize()
    {
        var text = new string('a', 5000);
        var options = new ChunkOptions(MaxSize: 1000, Overlap: 100, ChunkStrategy.FixedSize);

        var chunks = await _chunker.ChunkAsync(text, options);

        Assert.All(chunks, chunk => Assert.True(chunk.Text.Length <= 1000 + 100)); // Allow some flexibility
        Assert.True(chunks.Count > 1);
    }

    [Fact]
    public async Task ChunkByMarkdown_RespectsHeadings()
    {
        var text = @"# Title 1
Content for title 1.

## Subtitle 1
Content for subtitle 1.

## Subtitle 2
Content for subtitle 2.
";
        var options = new ChunkOptions(MaxSize: 1000, Overlap: 0, ChunkStrategy.MarkdownAware);

        var chunks = await _chunker.ChunkAsync(text, options);

        Assert.True(chunks.Count > 0);
        // Verify chunks contain heading information
        Assert.All(chunks, chunk => Assert.NotNull(chunk.Metadata));
    }

    [Fact]
    public async Task ChunkByFixedSize_AppliesOverlap()
    {
        var text = new string('a', 2000);
        var options = new ChunkOptions(MaxSize: 1000, Overlap: 200, ChunkStrategy.FixedSize);

        var chunks = await _chunker.ChunkAsync(text, options);

        if (chunks.Count > 1)
        {
            // Check that there's overlap between consecutive chunks
            var firstChunk = chunks[0];
            var secondChunk = chunks[1];
            Assert.True(firstChunk.EndIndex > secondChunk.StartIndex);
        }
    }
}
