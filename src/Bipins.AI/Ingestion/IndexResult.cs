namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Result of an indexing operation.
/// </summary>
/// <param name="ChunksIndexed">Number of chunks successfully indexed.</param>
/// <param name="VectorsCreated">Number of vectors created.</param>
/// <param name="Errors">List of error messages if any.</param>
public record IndexResult(
    int ChunksIndexed,
    int VectorsCreated,
    List<string>? Errors = null);
