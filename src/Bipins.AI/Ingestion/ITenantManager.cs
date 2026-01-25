namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Interface for managing tenants.
/// </summary>
public interface ITenantManager
{
    /// <summary>
    /// Gets tenant information.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tenant information, or null if not found.</returns>
    Task<TenantInfo?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new tenant.
    /// </summary>
    /// <param name="tenantInfo">Tenant information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task RegisterTenantAsync(TenantInfo tenantInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates tenant information.
    /// </summary>
    /// <param name="tenantInfo">Updated tenant information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task UpdateTenantAsync(TenantInfo tenantInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tenant exists.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if tenant exists, false otherwise.</returns>
    Task<bool> TenantExistsAsync(string tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a tenant.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="Name">The tenant name.</param>
/// <param name="CreatedAt">When the tenant was created.</param>
/// <param name="Quotas">Tenant quotas.</param>
/// <param name="Metadata">Additional metadata.</param>
public record TenantInfo(
    string TenantId,
    string Name,
    DateTimeOffset CreatedAt,
    TenantQuotas? Quotas = null,
    Dictionary<string, object>? Metadata = null);

/// <summary>
/// Quotas for a tenant.
/// </summary>
/// <param name="MaxDocuments">Maximum number of documents.</param>
/// <param name="MaxStorageBytes">Maximum storage in bytes.</param>
/// <param name="MaxRequestsPerDay">Maximum requests per day.</param>
/// <param name="MaxTokensPerRequest">Maximum tokens per request.</param>
public record TenantQuotas(
    int? MaxDocuments = null,
    long? MaxStorageBytes = null,
    int? MaxRequestsPerDay = null,
    int? MaxTokensPerRequest = null);
