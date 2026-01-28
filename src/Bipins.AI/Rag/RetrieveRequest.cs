using Bipins.AI.Vector;

namespace Bipins.AI.Core.Rag;

/// <summary>
/// Request to retrieve relevant chunks for RAG.
/// </summary>
/// <param name="Query">The query text.</param>
/// <param name="TenantId">Tenant identifier (required for multi-tenant isolation).</param>
/// <param name="TopK">Number of results to return.</param>
/// <param name="Filter">Optional filter to apply (will be combined with tenant filter).</param>
/// <param name="CollectionName">Optional collection name.</param>
public record RetrieveRequest(
    string Query,
    string TenantId,
    int TopK = 5,
    VectorFilter? Filter = null,
    string? CollectionName = null);
