namespace AICostOptimizationAdvisor.Shared.Models;

/// <summary>
/// Represents AWS cost data from Cost Explorer API.
/// </summary>
public class CostData
{
    public string Date { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public Dictionary<string, string>? Tags { get; set; }
    public string UsageType { get; set; } = string.Empty;
    public string UsageQuantity { get; set; } = string.Empty;
}

/// <summary>
/// Request model for fetching cost data.
/// </summary>
public class GetCostDataRequest
{
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string Granularity { get; set; } = "DAILY"; // DAILY or MONTHLY
    public List<string>? Services { get; set; }
    public List<string>? Regions { get; set; }
}

/// <summary>
/// Response model for cost data.
/// </summary>
public class GetCostDataResponse
{
    public List<CostData> Costs { get; set; } = new();
    public decimal TotalCost { get; set; }
    public string Currency { get; set; } = "USD";
    public string DateRange { get; set; } = string.Empty;
}
