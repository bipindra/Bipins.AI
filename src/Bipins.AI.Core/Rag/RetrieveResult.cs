namespace Bipins.AI.Core.Rag;

/// <summary>
/// Result of a retrieval operation.
/// </summary>
/// <param name="Chunks">List of retrieved chunks with scores.</param>
/// <param name="QueryVector">The query vector used for search.</param>
/// <param name="TotalMatches">Total number of matches found.</param>
public record RetrieveResult(
    IReadOnlyList<RagChunk> Chunks,
    ReadOnlyMemory<float> QueryVector,
    int TotalMatches);
