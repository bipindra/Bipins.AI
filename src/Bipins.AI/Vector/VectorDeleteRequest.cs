namespace Bipins.AI.Vector;

/// <summary>
/// Request to delete vector records.
/// </summary>
/// <param name="Ids">List of record IDs to delete. If empty and Filter is provided, deletes by filter.</param>
/// <param name="CollectionName">Optional collection name (uses default if not specified).</param>
/// <param name="Filter">Optional filter to delete records matching criteria.</param>
public record VectorDeleteRequest(
    IReadOnlyList<string> Ids,
    string? CollectionName = null,
    VectorFilter? Filter = null);
