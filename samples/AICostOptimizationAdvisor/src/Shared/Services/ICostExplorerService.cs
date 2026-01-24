using AICostOptimizationAdvisor.Shared.Models;

namespace AICostOptimizationAdvisor.Shared.Services;

/// <summary>
/// Service interface for interacting with AWS Cost Explorer API.
/// </summary>
public interface ICostExplorerService
{
    /// <summary>
    /// Gets cost and usage data from AWS Cost Explorer API.
    /// </summary>
    Task<GetCostDataResponse> GetCostAndUsageAsync(GetCostDataRequest request, CancellationToken cancellationToken = default);
}
