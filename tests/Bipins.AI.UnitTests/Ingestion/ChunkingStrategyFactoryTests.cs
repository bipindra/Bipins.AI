using Bipins.AI.Core.Ingestion;
using Bipins.AI.Ingestion;
using Bipins.AI.Ingestion.Strategies;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Ingestion;

public class ChunkingStrategyFactoryTests
{
    [Fact]
    public void DefaultChunkingStrategyFactory_GetStrategy_ReturnsCorrectStrategy()
    {
        var fixedSizeLogger = new Mock<ILogger<FixedSizeChunkingStrategy>>();
        var sentenceLogger = new Mock<ILogger<SentenceAwareChunkingStrategy>>();
        var paragraphLogger = new Mock<ILogger<ParagraphChunkingStrategy>>();
        var markdownLogger = new Mock<ILogger<MarkdownAwareChunkingStrategy>>();
        var factoryLogger = new Mock<ILogger<DefaultChunkingStrategyFactory>>();

        var strategies = new IChunkingStrategy[]
        {
            new FixedSizeChunkingStrategy(fixedSizeLogger.Object),
            new SentenceAwareChunkingStrategy(sentenceLogger.Object),
            new ParagraphChunkingStrategy(paragraphLogger.Object),
            new MarkdownAwareChunkingStrategy(markdownLogger.Object)
        };

        var factory = new DefaultChunkingStrategyFactory(strategies, factoryLogger.Object);

        var fixedSizeStrategy = factory.GetStrategy(ChunkStrategy.FixedSize);
        Assert.IsType<FixedSizeChunkingStrategy>(fixedSizeStrategy);

        var sentenceStrategy = factory.GetStrategy(ChunkStrategy.Sentence);
        Assert.IsType<SentenceAwareChunkingStrategy>(sentenceStrategy);

        var paragraphStrategy = factory.GetStrategy(ChunkStrategy.Paragraph);
        Assert.IsType<ParagraphChunkingStrategy>(paragraphStrategy);

        var markdownStrategy = factory.GetStrategy(ChunkStrategy.MarkdownAware);
        Assert.IsType<MarkdownAwareChunkingStrategy>(markdownStrategy);
    }

    [Fact]
    public void DefaultChunkingStrategyFactory_GetStrategy_WithUnknownStrategy_FallsBackToFixedSize()
    {
        var fixedSizeLogger = new Mock<ILogger<FixedSizeChunkingStrategy>>();
        var factoryLogger = new Mock<ILogger<DefaultChunkingStrategyFactory>>();

        var strategies = new IChunkingStrategy[]
        {
            new FixedSizeChunkingStrategy(fixedSizeLogger.Object)
        };

        var factory = new DefaultChunkingStrategyFactory(strategies, factoryLogger.Object);

        // Try to get an unknown strategy - should fallback to FixedSize
        var strategy = factory.GetStrategy((ChunkStrategy)999);

        Assert.IsType<FixedSizeChunkingStrategy>(strategy);
    }
}
