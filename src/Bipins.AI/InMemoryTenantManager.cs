using Bipins.AI.Core.Ingestion;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Ingestion;

/// <summary>
/// In-memory tenant manager (for development/testing).
/// </summary>
public class InMemoryTenantManager : ITenantManager
{
    private readonly ILogger<InMemoryTenantManager> _logger;
    private readonly Dictionary<string, TenantInfo> _tenants = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTenantManager"/> class.
    /// </summary>
    public InMemoryTenantManager(ILogger<InMemoryTenantManager> logger)
    {
        _logger = logger;
        
        // Add default tenant
        _tenants["default"] = new TenantInfo(
            "default",
            "Default Tenant",
            DateTimeOffset.UtcNow,
            new TenantQuotas(
                MaxDocuments: 10000,
                MaxStorageBytes: 10_000_000_000, // 10 GB
                MaxRequestsPerDay: 100000,
                MaxTokensPerRequest: 100000));
    }

    /// <inheritdoc />
    public Task<TenantInfo?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        _tenants.TryGetValue(tenantId, out var tenant);
        return Task.FromResult<TenantInfo?>(tenant);
    }

    /// <inheritdoc />
    public Task RegisterTenantAsync(TenantInfo tenantInfo, CancellationToken cancellationToken = default)
    {
        TenantValidator.ValidateOrThrow(tenantInfo.TenantId);
        _tenants[tenantInfo.TenantId] = tenantInfo;
        _logger.LogInformation("Registered tenant {TenantId}", tenantInfo.TenantId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateTenantAsync(TenantInfo tenantInfo, CancellationToken cancellationToken = default)
    {
        if (!_tenants.ContainsKey(tenantInfo.TenantId))
        {
            throw new InvalidOperationException($"Tenant {tenantInfo.TenantId} does not exist");
        }

        _tenants[tenantInfo.TenantId] = tenantInfo;
        _logger.LogInformation("Updated tenant {TenantId}", tenantInfo.TenantId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> TenantExistsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_tenants.ContainsKey(tenantId));
    }
}
