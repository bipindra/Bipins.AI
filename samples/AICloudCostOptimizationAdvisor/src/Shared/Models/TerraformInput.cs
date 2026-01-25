namespace AICloudCostOptimizationAdvisor.Shared.Models;

/// <summary>
/// Input model for Terraform script submission.
/// </summary>
public class TerraformInput
{
    /// <summary>
    /// Terraform script content (when pasted as text).
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// URL to Terraform file(s).
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Cloud providers to analyze (AWS, Azure, GCP).
    /// </summary>
    public List<string> CloudProviders { get; set; } = new();

    /// <summary>
    /// Analysis options.
    /// </summary>
    public AnalysisOptions? Options { get; set; }
}

/// <summary>
/// Analysis options for cost calculation.
/// </summary>
public class AnalysisOptions
{
    /// <summary>
    /// Time period for cost estimation (Monthly, Annual).
    /// </summary>
    public string TimePeriod { get; set; } = "Monthly";

    /// <summary>
    /// Region for cost calculation (defaults to us-east-1 for AWS, etc.).
    /// </summary>
    public string? DefaultRegion { get; set; }

    /// <summary>
    /// Include optimization suggestions.
    /// </summary>
    public bool IncludeOptimizations { get; set; } = true;

    /// <summary>
    /// Include security risk analysis.
    /// </summary>
    public bool IncludeSecurityRisks { get; set; } = false;

    /// <summary>
    /// Include Mermaid.js diagrams for before/after infrastructure visualization.
    /// </summary>
    public bool IncludeMermaidDiagrams { get; set; } = false;
}
