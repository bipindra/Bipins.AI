namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Interface for enforcing tenant quotas.
/// </summary>
public interface ITenantQuotaEnforcer
{
    /// <summary>
    /// Checks if a document ingestion is allowed for the tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if allowed, false otherwise.</returns>
    Task<bool> CanIngestDocumentAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a chat request is allowed for the tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="estimatedTokens">Estimated token count for the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if allowed, false otherwise.</returns>
    Task<bool> CanMakeChatRequestAsync(string tenantId, int estimatedTokens, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a document ingestion for quota tracking.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="chunkCount">Number of chunks ingested.</param>
    /// <param name="storageBytes">Storage bytes used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task RecordDocumentIngestionAsync(string tenantId, int chunkCount, long storageBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a chat request for quota tracking.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="tokensUsed">Tokens used in the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task RecordChatRequestAsync(string tenantId, int tokensUsed, CancellationToken cancellationToken = default);
}
