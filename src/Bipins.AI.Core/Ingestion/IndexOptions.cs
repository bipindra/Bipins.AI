namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Options for indexing chunks.
/// </summary>
/// <param name="TenantId">Tenant identifier.</param>
/// <param name="DocId">Document identifier.</param>
/// <param name="VersionId">Version identifier for document versioning.</param>
/// <param name="CollectionName">Vector store collection name.</param>
public record IndexOptions(
    string TenantId,
    string? DocId = null,
    string? VersionId = null,
    string? CollectionName = null);
