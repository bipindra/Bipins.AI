namespace Bipins.AI.Vector;

/// <summary>
/// A vector record stored in a vector database.
/// </summary>
/// <param name="Id">Unique identifier for the record.</param>
/// <param name="Vector">The embedding vector.</param>
/// <param name="Text">The original text that was embedded.</param>
/// <param name="Metadata">Additional metadata.</param>
/// <param name="SourceUri">Source URI of the document.</param>
/// <param name="DocId">Document identifier.</param>
/// <param name="ChunkId">Chunk identifier within the document.</param>
/// <param name="TenantId">Tenant identifier.</param>
/// <param name="VersionId">Version identifier for document versioning.</param>
public record VectorRecord(
    string Id,
    ReadOnlyMemory<float> Vector,
    string Text,
    Dictionary<string, object>? Metadata = null,
    string? SourceUri = null,
    string? DocId = null,
    string? ChunkId = null,
    string? TenantId = null,
    string? VersionId = null);
