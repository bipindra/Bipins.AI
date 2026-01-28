using Bipins.AI.Core.Ingestion;
using Bipins.AI.Ingestion.Strategies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bipins.AI.Ingestion;

/// <summary>
/// Default implementation of chunking strategy factory.
/// </summary>
public class DefaultChunkingStrategyFactory : IChunkingStrategyFactory
{
    private readonly Dictionary<ChunkStrategy, IChunkingStrategy> _strategies;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultChunkingStrategyFactory"/> class.
    /// </summary>
    public DefaultChunkingStrategyFactory(
        IEnumerable<IChunkingStrategy> strategies,
        ILogger<DefaultChunkingStrategyFactory> logger)
    {
        _strategies = strategies.ToDictionary(s => s.StrategyType, s => s);
        logger.LogInformation("Registered {Count} chunking strategies", _strategies.Count);
    }

    /// <inheritdoc />
    public IChunkingStrategy GetStrategy(ChunkStrategy strategy)
    {
        if (_strategies.TryGetValue(strategy, out var strategyImpl))
        {
            return strategyImpl;
        }

        // Fallback to FixedSize if strategy not found
        if (_strategies.TryGetValue(ChunkStrategy.FixedSize, out var fallback))
        {
            return fallback;
        }

        // Ultimate fallback: construct a default fixed-size strategy if none were registered
        return new FixedSizeChunkingStrategy(NullLogger<FixedSizeChunkingStrategy>.Instance);
    }
}
