namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Options for text chunking.
/// </summary>
/// <param name="MaxSize">Maximum chunk size in characters.</param>
/// <param name="Overlap">Overlap size in characters between chunks.</param>
/// <param name="Strategy">Chunking strategy.</param>
public record ChunkOptions(
    int MaxSize = 1000,
    int Overlap = 200,
    ChunkStrategy Strategy = ChunkStrategy.FixedSize);
