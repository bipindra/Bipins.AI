using Bipins.AI.Core.Ingestion;
using Bipins.AI.Vector;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Ingestion;

/// <summary>
/// Manages document versions using the vector store as the source of truth.
/// </summary>
public class VectorStoreDocumentVersionManager : IDocumentVersionManager
{
    private readonly ILogger<VectorStoreDocumentVersionManager> _logger;
    private readonly IVectorStore _vectorStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="VectorStoreDocumentVersionManager"/> class.
    /// </summary>
    public VectorStoreDocumentVersionManager(
        ILogger<VectorStoreDocumentVersionManager> logger,
        IVectorStore vectorStore)
    {
        _logger = logger;
        _vectorStore = vectorStore;
    }

    /// <inheritdoc />
    public async Task<List<DocumentVersion>> ListVersionsAsync(
        string tenantId,
        string docId,
        string? collectionName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Query for all chunks with this docId and tenantId
            var filter = new VectorFilterAnd(new[]
            {
                new VectorFilterPredicate(new FilterPredicate("docId", FilterOperator.Eq, docId)),
                new VectorFilterPredicate(new FilterPredicate("tenantId", FilterOperator.Eq, tenantId))
            });

            // Use a dummy vector for filtering (dimension 1536 is common for OpenAI embeddings)
            var dummyVector = new float[1536].AsMemory();
            var queryRequest = new VectorQueryRequest(
                dummyVector,
                TopK: 10000, // Large number to get all matches
                tenantId,
                filter,
                collectionName);

            var queryResponse = await _vectorStore.QueryAsync(queryRequest, cancellationToken);

            // Group by versionId and aggregate metadata
            var versionGroups = queryResponse.Matches
                .Select(m => m.Record)
                .Where(r => r.VersionId != null)
                .GroupBy(r => r.VersionId!)
                .ToList();

            var versions = new List<DocumentVersion>();
            foreach (var group in versionGroups)
            {
                var versionId = group.Key;
                var records = group.ToList();
                var firstRecord = records.First();

                // Extract createdAt from metadata if available
                var createdAt = DateTime.UtcNow;
                if (firstRecord.Metadata != null && firstRecord.Metadata.TryGetValue("createdAt", out var createdAtObj))
                {
                    if (createdAtObj is DateTime dt)
                    {
                        createdAt = dt;
                    }
                    else if (createdAtObj is string dtStr && DateTime.TryParse(dtStr, out var parsed))
                    {
                        createdAt = parsed;
                    }
                }

                var version = new DocumentVersion(
                    versionId,
                    docId,
                    tenantId,
                    createdAt,
                    records.Count,
                    firstRecord.Metadata);

                versions.Add(version);
            }

            // Sort by creation date descending (newest first)
            versions = versions.OrderByDescending(v => v.CreatedAt).ToList();

            _logger.LogInformation(
                "Found {Count} versions for document {DocId} in tenant {TenantId}",
                versions.Count,
                docId,
                tenantId);

            return versions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing versions for document {DocId}", docId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentVersion?> GetVersionAsync(
        string tenantId,
        string docId,
        string versionId,
        string? collectionName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Query for chunks with this docId, tenantId, and versionId
            var filter = new VectorFilterAnd(new[]
            {
                new VectorFilterPredicate(new FilterPredicate("docId", FilterOperator.Eq, docId)),
                new VectorFilterPredicate(new FilterPredicate("tenantId", FilterOperator.Eq, tenantId)),
                new VectorFilterPredicate(new FilterPredicate("versionId", FilterOperator.Eq, versionId))
            });

            // Use a dummy vector for filtering
            var dummyVector = new float[1536].AsMemory();
            var queryRequest = new VectorQueryRequest(
                dummyVector,
                TopK: 10000,
                tenantId,
                filter,
                collectionName);

            var queryResponse = await _vectorStore.QueryAsync(queryRequest, cancellationToken);

            var records = queryResponse.Matches.Select(m => m.Record).ToList();
            if (records.Count == 0)
            {
                return null;
            }

            var firstRecord = records.First();

            // Extract createdAt from metadata if available
            var createdAt = DateTime.UtcNow;
            if (firstRecord.Metadata != null && firstRecord.Metadata.TryGetValue("createdAt", out var createdAtObj))
            {
                if (createdAtObj is DateTime dt)
                {
                    createdAt = dt;
                }
                else if (createdAtObj is string dtStr && DateTime.TryParse(dtStr, out var parsed))
                {
                    createdAt = parsed;
                }
            }

            return new DocumentVersion(
                versionId,
                docId,
                tenantId,
                createdAt,
                records.Count,
                firstRecord.Metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting version {VersionId} for document {DocId}", versionId, docId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> GenerateVersionIdAsync(
        string tenantId,
        string docId,
        CancellationToken cancellationToken = default)
    {
        // Generate a version ID based on timestamp and a random component
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = Guid.NewGuid().ToString("N")[..8];
        return $"{timestamp}-{random}";
    }
}
