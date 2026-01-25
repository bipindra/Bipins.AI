namespace AICloudCostOptimizationAdvisor.Shared.Models;

/// <summary>
/// Complete Terraform cost analysis result.
/// </summary>
public class TerraformAnalysis
{
    /// <summary>
    /// Unique analysis ID.
    /// </summary>
    public string AnalysisId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp when analysis was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Original Terraform script content (truncated if too long).
    /// </summary>
    public string? TerraformContent { get; set; }

    /// <summary>
    /// Cost breakdown by cloud provider.
    /// </summary>
    public List<CloudCost> CloudCosts { get; set; } = new();

    /// <summary>
    /// Total monthly cost across all providers.
    /// </summary>
    public decimal TotalMonthlyCost { get; set; }

    /// <summary>
    /// Total annual cost across all providers.
    /// </summary>
    public decimal TotalAnnualCost { get; set; }

    /// <summary>
    /// AI-generated optimization suggestions.
    /// </summary>
    public List<OptimizationSuggestion> Optimizations { get; set; } = new();

    /// <summary>
    /// Summary of the analysis.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Parsed resources from Terraform.
    /// </summary>
    public List<ParsedResource> ParsedResources { get; set; } = new();

    /// <summary>
    /// Analysis status (Success, Error, InProgress).
    /// </summary>
    public string Status { get; set; } = "Success";

    /// <summary>
    /// Error message if analysis failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Security risks identified in the infrastructure.
    /// </summary>
    public List<SecurityRisk> SecurityRisks { get; set; } = new();

    /// <summary>
    /// Mermaid.js diagram code for the current (before optimization) infrastructure architecture.
    /// </summary>
    public string? MermaidDiagramBefore { get; set; }

    /// <summary>
    /// Mermaid.js diagram code for the optimized (after optimization) infrastructure architecture.
    /// </summary>
    public string? MermaidDiagramAfter { get; set; }
}

/// <summary>
/// Represents a parsed Terraform resource.
/// </summary>
public class ParsedResource
{
    /// <summary>
    /// Resource identifier.
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Resource type.
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Cloud provider.
    /// </summary>
    public string CloudProvider { get; set; } = string.Empty;

    /// <summary>
    /// Resource attributes.
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = new();
}
