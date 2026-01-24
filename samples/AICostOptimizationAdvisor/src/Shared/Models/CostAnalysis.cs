namespace AICostOptimizationAdvisor.Shared.Models;

/// <summary>
/// Represents a cost driver identified in the analysis.
/// </summary>
public class CostDriver
{
    public string Service { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents an anomaly detected in cost data.
/// </summary>
public class CostAnomaly
{
    public string Date { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Variance { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents an optimization suggestion.
/// </summary>
public class OptimizationSuggestion
{
    public string Category { get; set; } = string.Empty; // Compute, Storage, Network, Other
    public string Description { get; set; } = string.Empty;
    public string EstimatedSavings { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium"; // High, Medium, Low
    public List<string>? Actions { get; set; }
    public string Service { get; set; } = string.Empty;
}

/// <summary>
/// Complete cost analysis result.
/// </summary>
public class CostAnalysis
{
    public string AnalysisId { get; set; } = string.Empty;
    public string DateRange { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<CostDriver> CostDrivers { get; set; } = new();
    public List<CostAnomaly> Anomalies { get; set; } = new();
    public List<OptimizationSuggestion> Suggestions { get; set; } = new();
    public decimal TotalCost { get; set; }
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Request model for cost analysis.
/// </summary>
public class AnalyzeCostsRequest
{
    public GetCostDataResponse CostData { get; set; } = new();
    public string? ModelId { get; set; }
}

/// <summary>
/// Response model for cost analysis.
/// </summary>
public class AnalyzeCostsResponse
{
    public CostAnalysis Analysis { get; set; } = new();
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
