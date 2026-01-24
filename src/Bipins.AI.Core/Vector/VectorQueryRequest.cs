namespace Bipins.AI.Core.Vector;

/// <summary>
/// Request to query a vector store.
/// </summary>
/// <param name="QueryVector">The query vector to search for.</param>
/// <param name="TopK">Number of results to return.</param>
/// <param name="Filter">Optional filter to apply.</param>
/// <param name="CollectionName">Optional collection name (uses default if not specified).</param>
public record VectorQueryRequest(
    ReadOnlyMemory<float> QueryVector,
    int TopK,
    VectorFilter? Filter = null,
    string? CollectionName = null);
