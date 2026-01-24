namespace Bipins.AI.Core.CostTracking;

/// <summary>
/// Interface for tracking costs.
/// </summary>
public interface ICostTracker
{
    /// <summary>
    /// Records a cost for an operation.
    /// </summary>
    /// <param name="record">The cost record to track.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task RecordCostAsync(CostRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cost records for a tenant within a time range.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="startTime">Start of the time range.</param>
    /// <param name="endTime">End of the time range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of cost records.</returns>
    Task<IReadOnlyList<CostRecord>> GetCostRecordsAsync(
        string tenantId,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated cost summary for a tenant within a time range.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="startTime">Start of the time range.</param>
    /// <param name="endTime">End of the time range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cost summary.</returns>
    Task<CostSummary> GetCostSummaryAsync(
        string tenantId,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Summary of costs for a tenant.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="StartTime">Start of the time range.</param>
/// <param name="EndTime">End of the time range.</param>
/// <param name="TotalCost">Total cost in USD.</param>
/// <param name="TotalTokens">Total tokens used.</param>
/// <param name="TotalApiCalls">Total API calls made.</param>
/// <param name="TotalStorageBytes">Total storage bytes used.</param>
/// <param name="CostByOperation">Cost breakdown by operation type.</param>
/// <param name="CostByProvider">Cost breakdown by provider.</param>
/// <param name="CostByModel">Cost breakdown by model.</param>
public record CostSummary(
    string TenantId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    decimal TotalCost,
    long TotalTokens,
    int TotalApiCalls,
    long TotalStorageBytes,
    Dictionary<CostOperationType, decimal> CostByOperation,
    Dictionary<string, decimal> CostByProvider,
    Dictionary<string, decimal> CostByModel);
