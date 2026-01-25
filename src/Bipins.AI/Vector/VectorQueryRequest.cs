namespace Bipins.AI.Core.Vector;

/// <summary>
/// Request to query a vector store.
/// </summary>
/// <param name="QueryVector">The query vector to search for.</param>
/// <param name="TopK">Number of results to return.</param>
/// <param name="TenantId">Tenant identifier (required for multi-tenant isolation).</param>
/// <param name="Filter">Optional filter to apply (will be combined with tenant filter).</param>
/// <param name="CollectionName">Optional collection name (uses default if not specified).</param>
public record VectorQueryRequest(
    ReadOnlyMemory<float> QueryVector,
    int TopK,
    string TenantId,
    VectorFilter? Filter = null,
    string? CollectionName = null);
