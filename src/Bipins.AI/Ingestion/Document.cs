namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Represents a document loaded from a source.
/// </summary>
/// <param name="SourceUri">The source URI of the document.</param>
/// <param name="Content">The document content as bytes.</param>
/// <param name="MimeType">The MIME type of the document.</param>
/// <param name="Metadata">Additional metadata about the document.</param>
public record Document(
    string SourceUri,
    byte[] Content,
    string MimeType,
    Dictionary<string, object>? Metadata = null);
