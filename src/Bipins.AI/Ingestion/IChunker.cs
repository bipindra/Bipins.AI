namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Contract for chunking text into smaller pieces.
/// </summary>
public interface IChunker
{
    /// <summary>
    /// Chunks text according to the specified options.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="options">Chunking options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of chunks.</returns>
    Task<IReadOnlyList<Chunk>> ChunkAsync(string text, ChunkOptions options, CancellationToken cancellationToken = default);
}
