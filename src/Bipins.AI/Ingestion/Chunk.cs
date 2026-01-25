namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Represents a text chunk extracted from a document.
/// </summary>
/// <param name="Id">Unique identifier for the chunk.</param>
/// <param name="Text">The chunk text content.</param>
/// <param name="StartIndex">Starting character index in the original document.</param>
/// <param name="EndIndex">Ending character index in the original document.</param>
/// <param name="Metadata">Additional metadata about the chunk.</param>
public record Chunk(
    string Id,
    string Text,
    int StartIndex,
    int EndIndex,
    Dictionary<string, object>? Metadata = null);
