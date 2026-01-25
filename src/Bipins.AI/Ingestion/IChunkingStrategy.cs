namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Interface for chunking strategies.
/// </summary>
public interface IChunkingStrategy
{
    /// <summary>
    /// Gets the strategy type this implementation handles.
    /// </summary>
    ChunkStrategy StrategyType { get; }

    /// <summary>
    /// Chunks text according to the strategy.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="options">Chunking options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of chunks.</returns>
    Task<IReadOnlyList<Chunk>> ChunkAsync(string text, ChunkOptions options, CancellationToken cancellationToken = default);
}
