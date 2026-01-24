using Bipins.AI.Core.Vector;

namespace Bipins.AI.Core.Rag;

/// <summary>
/// Request to retrieve relevant chunks for RAG.
/// </summary>
/// <param name="Query">The query text.</param>
/// <param name="TopK">Number of results to return.</param>
/// <param name="Filter">Optional filter to apply.</param>
/// <param name="CollectionName">Optional collection name.</param>
public record RetrieveRequest(
    string Query,
    int TopK = 5,
    VectorFilter? Filter = null,
    string? CollectionName = null);
