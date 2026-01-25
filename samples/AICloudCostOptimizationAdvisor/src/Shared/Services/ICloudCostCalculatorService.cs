using AICloudCostOptimizationAdvisor.Shared.Models;

namespace AICloudCostOptimizationAdvisor.Shared.Services;

/// <summary>
/// Service interface for calculating cloud costs from Terraform resources.
/// </summary>
public interface ICloudCostCalculatorService
{
    /// <summary>
    /// Calculates costs for parsed resources grouped by cloud provider.
    /// </summary>
    Task<List<CloudCost>> CalculateCostsAsync(
        List<ParsedResource> resources,
        string timePeriod = "Monthly",
        CancellationToken cancellationToken = default);
}
