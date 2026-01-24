using Bipins.AI.Core.Ingestion;

namespace Bipins.AI.Core.Rag;

/// <summary>
/// A chunk retrieved for RAG with relevance information.
/// </summary>
/// <param name="Chunk">The base chunk information.</param>
/// <param name="Score">The relevance score (0-1).</param>
/// <param name="SourceUri">Source URI of the document.</param>
/// <param name="DocId">Document identifier.</param>
public record RagChunk(
    Chunk Chunk,
    float Score,
    string? SourceUri = null,
    string? DocId = null);
