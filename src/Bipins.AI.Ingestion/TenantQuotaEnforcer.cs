using Bipins.AI.Core.Ingestion;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Ingestion;

/// <summary>
/// Enforces tenant quotas.
/// </summary>
public class TenantQuotaEnforcer : ITenantQuotaEnforcer
{
    private readonly ITenantManager _tenantManager;
    private readonly ILogger<TenantQuotaEnforcer> _logger;
    private readonly Dictionary<string, TenantQuotaUsage> _usage = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantQuotaEnforcer"/> class.
    /// </summary>
    public TenantQuotaEnforcer(
        ITenantManager tenantManager,
        ILogger<TenantQuotaEnforcer> logger)
    {
        _tenantManager = tenantManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> CanIngestDocumentAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantManager.GetTenantAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            _logger.LogWarning("Tenant {TenantId} not found", tenantId);
            return false;
        }

        if (tenant.Quotas == null)
        {
            return true; // No quotas configured
        }

        var usage = GetOrCreateUsage(tenantId);
        var quotas = tenant.Quotas;

        // Check document count
        if (quotas.MaxDocuments.HasValue && usage.DocumentCount >= quotas.MaxDocuments.Value)
        {
            _logger.LogWarning(
                "Tenant {TenantId} exceeded max documents: {Count}/{Max}",
                tenantId,
                usage.DocumentCount,
                quotas.MaxDocuments.Value);
            return false;
        }

        // Check storage
        if (quotas.MaxStorageBytes.HasValue && usage.StorageBytes >= quotas.MaxStorageBytes.Value)
        {
            _logger.LogWarning(
                "Tenant {TenantId} exceeded max storage: {Bytes}/{MaxBytes}",
                tenantId,
                usage.StorageBytes,
                quotas.MaxStorageBytes.Value);
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> CanMakeChatRequestAsync(string tenantId, int estimatedTokens, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantManager.GetTenantAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            _logger.LogWarning("Tenant {TenantId} not found", tenantId);
            return false;
        }

        if (tenant.Quotas == null)
        {
            return true; // No quotas configured
        }

        var quotas = tenant.Quotas;

        // Check tokens per request
        if (quotas.MaxTokensPerRequest.HasValue && estimatedTokens > quotas.MaxTokensPerRequest.Value)
        {
            _logger.LogWarning(
                "Tenant {TenantId} request exceeds max tokens per request: {Tokens}/{Max}",
                tenantId,
                estimatedTokens,
                quotas.MaxTokensPerRequest.Value);
            return false;
        }

        // Check daily request limit
        var usage = GetOrCreateUsage(tenantId);
        if (quotas.MaxRequestsPerDay.HasValue)
        {
            // Reset daily count if it's a new day
            if (usage.LastRequestDate.Date < DateTime.UtcNow.Date)
            {
                usage.RequestsToday = 0;
                usage.LastRequestDate = DateTime.UtcNow;
            }

            if (usage.RequestsToday >= quotas.MaxRequestsPerDay.Value)
            {
                _logger.LogWarning(
                    "Tenant {TenantId} exceeded max requests per day: {Count}/{Max}",
                    tenantId,
                    usage.RequestsToday,
                    quotas.MaxRequestsPerDay.Value);
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public Task RecordDocumentIngestionAsync(string tenantId, int chunkCount, long storageBytes, CancellationToken cancellationToken = default)
    {
        var usage = GetOrCreateUsage(tenantId);
        usage.DocumentCount++;
        usage.StorageBytes += storageBytes;
        _logger.LogDebug(
            "Recorded ingestion for tenant {TenantId}: {Chunks} chunks, {Bytes} bytes",
            tenantId,
            chunkCount,
            storageBytes);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RecordChatRequestAsync(string tenantId, int tokensUsed, CancellationToken cancellationToken = default)
    {
        var usage = GetOrCreateUsage(tenantId);
        
        // Reset daily count if it's a new day
        if (usage.LastRequestDate.Date < DateTime.UtcNow.Date)
        {
            usage.RequestsToday = 0;
            usage.LastRequestDate = DateTime.UtcNow;
        }

        usage.RequestsToday++;
        usage.TotalTokensUsed += tokensUsed;
        
        _logger.LogDebug(
            "Recorded chat request for tenant {TenantId}: {Tokens} tokens, {RequestsToday} requests today",
            tenantId,
            tokensUsed,
            usage.RequestsToday);
        return Task.CompletedTask;
    }

    private TenantQuotaUsage GetOrCreateUsage(string tenantId)
    {
        lock (_usage)
        {
            if (!_usage.TryGetValue(tenantId, out var usage))
            {
                usage = new TenantQuotaUsage();
                _usage[tenantId] = usage;
            }
            return usage;
        }
    }

    private class TenantQuotaUsage
    {
        public int DocumentCount { get; set; }
        public long StorageBytes { get; set; }
        public int RequestsToday { get; set; }
        public long TotalTokensUsed { get; set; }
        public DateTime LastRequestDate { get; set; } = DateTime.UtcNow;
    }
}
