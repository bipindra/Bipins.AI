namespace Bipins.AI.Vector;

/// <summary>
/// Request to upsert vector records.
/// </summary>
/// <param name="Records">List of vector records to upsert.</param>
/// <param name="CollectionName">Optional collection name (uses default if not specified).</param>
public record VectorUpsertRequest(
    IReadOnlyList<VectorRecord> Records,
    string? CollectionName = null);
