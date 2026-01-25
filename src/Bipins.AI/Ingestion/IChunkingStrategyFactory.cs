namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Factory for creating chunking strategies.
/// </summary>
public interface IChunkingStrategyFactory
{
    /// <summary>
    /// Gets a chunking strategy for the specified strategy type.
    /// </summary>
    /// <param name="strategy">The chunking strategy type.</param>
    /// <returns>The chunking strategy implementation.</returns>
    IChunkingStrategy GetStrategy(ChunkStrategy strategy);
}
