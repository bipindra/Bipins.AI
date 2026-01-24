namespace Bipins.AI.Core.Vector;

/// <summary>
/// Request to delete vector records.
/// </summary>
/// <param name="Ids">List of record IDs to delete.</param>
/// <param name="CollectionName">Optional collection name (uses default if not specified).</param>
public record VectorDeleteRequest(
    IReadOnlyList<string> Ids,
    string? CollectionName = null);
