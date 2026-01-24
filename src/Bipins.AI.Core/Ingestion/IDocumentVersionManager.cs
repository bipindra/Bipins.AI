namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Manages document versions and version history.
/// </summary>
public interface IDocumentVersionManager
{
    /// <summary>
    /// Lists all versions for a document.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="docId">Document identifier.</param>
    /// <param name="collectionName">Optional collection name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of document versions with metadata.</returns>
    Task<List<DocumentVersion>> ListVersionsAsync(
        string tenantId,
        string docId,
        string? collectionName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version of a document.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="docId">Document identifier.</param>
    /// <param name="versionId">Version identifier.</param>
    /// <param name="collectionName">Optional collection name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Document version information, or null if not found.</returns>
    Task<DocumentVersion?> GetVersionAsync(
        string tenantId,
        string docId,
        string versionId,
        string? collectionName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a new version ID for a document.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="docId">Document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new version ID.</returns>
    Task<string> GenerateVersionIdAsync(
        string tenantId,
        string docId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a document version with metadata.
/// </summary>
/// <param name="VersionId">Version identifier.</param>
/// <param name="DocId">Document identifier.</param>
/// <param name="TenantId">Tenant identifier.</param>
/// <param name="CreatedAt">When this version was created.</param>
/// <param name="ChunkCount">Number of chunks in this version.</param>
/// <param name="Metadata">Additional metadata.</param>
public record DocumentVersion(
    string VersionId,
    string DocId,
    string TenantId,
    DateTime CreatedAt,
    int ChunkCount,
    Dictionary<string, object>? Metadata = null);
