namespace Bipins.AI.Core.Contracts;

/// <summary>
/// Represents a citation from a retrieved document chunk.
/// </summary>
/// <param name="SourceUri">The source URI of the document.</param>
/// <param name="DocId">The document identifier.</param>
/// <param name="ChunkId">The chunk identifier within the document.</param>
/// <param name="Text">The text excerpt from the chunk.</param>
/// <param name="Score">The relevance score (0-1).</param>
public record Citation(
    string? SourceUri,
    string? DocId,
    string? ChunkId,
    string Text,
    float Score);
