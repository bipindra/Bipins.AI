using Bipins.AI.Core.CostTracking;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Runtime.CostTracking;

/// <summary>
/// In-memory implementation of cost tracker (for development/testing).
/// </summary>
public class InMemoryCostTracker : ICostTracker
{
    private readonly ILogger<InMemoryCostTracker> _logger;
    private readonly List<CostRecord> _records = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryCostTracker"/> class.
    /// </summary>
    public InMemoryCostTracker(ILogger<InMemoryCostTracker> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task RecordCostAsync(CostRecord record, CancellationToken cancellationToken = default)
    {
        lock (_records)
        {
            _records.Add(record);
        }

        _logger.LogDebug(
            "Recorded cost: Tenant={TenantId}, Operation={OperationType}, Cost=${Cost}, Tokens={Tokens}",
            record.TenantId,
            record.OperationType,
            record.Cost,
            record.TokensUsed);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CostRecord>> GetCostRecordsAsync(
        string tenantId,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        CancellationToken cancellationToken = default)
    {
        lock (_records)
        {
            var filtered = _records
                .Where(r => r.TenantId == tenantId &&
                           r.Timestamp >= startTime &&
                           r.Timestamp <= endTime)
                .OrderBy(r => r.Timestamp)
                .ToList();

            return Task.FromResult<IReadOnlyList<CostRecord>>(filtered);
        }
    }

    /// <inheritdoc />
    public Task<CostSummary> GetCostSummaryAsync(
        string tenantId,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        CancellationToken cancellationToken = default)
    {
        lock (_records)
        {
            var filtered = _records
                .Where(r => r.TenantId == tenantId &&
                           r.Timestamp >= startTime &&
                           r.Timestamp <= endTime)
                .ToList();

            var totalCost = filtered.Sum(r => r.Cost);
            var totalTokens = filtered.Sum(r => r.TokensUsed ?? 0);
            var totalApiCalls = filtered.Sum(r => r.ApiCalls);
            var totalStorageBytes = filtered.Sum(r => r.StorageBytes ?? 0);

            var costByOperation = filtered
                .GroupBy(r => r.OperationType)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Cost));

            var costByProvider = filtered
                .GroupBy(r => r.Provider)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Cost));

            var costByModel = filtered
                .Where(r => !string.IsNullOrEmpty(r.ModelId))
                .GroupBy(r => r.ModelId!)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Cost));

            var summary = new CostSummary(
                tenantId,
                startTime,
                endTime,
                totalCost,
                totalTokens,
                totalApiCalls,
                totalStorageBytes,
                costByOperation,
                costByProvider,
                costByModel);

            return Task.FromResult(summary);
        }
    }
}
