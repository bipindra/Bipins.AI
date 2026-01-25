namespace AICloudCostOptimizationAdvisor.Shared.Models;

/// <summary>
/// Cost breakdown for a specific cloud provider.
/// </summary>
public class CloudCost
{
    /// <summary>
    /// Cloud provider name (AWS, Azure, GCP).
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Total monthly cost for this provider.
    /// </summary>
    public decimal TotalMonthlyCost { get; set; }

    /// <summary>
    /// Total annual cost for this provider.
    /// </summary>
    public decimal TotalAnnualCost { get; set; }

    /// <summary>
    /// Cost breakdown by service.
    /// </summary>
    public Dictionary<string, decimal> CostByService { get; set; } = new();

    /// <summary>
    /// Cost breakdown by region.
    /// </summary>
    public Dictionary<string, decimal> CostByRegion { get; set; } = new();

    /// <summary>
    /// List of individual resource costs.
    /// </summary>
    public List<ResourceCost> Resources { get; set; } = new();

    /// <summary>
    /// Number of resources analyzed.
    /// </summary>
    public int ResourceCount { get; set; }
}
