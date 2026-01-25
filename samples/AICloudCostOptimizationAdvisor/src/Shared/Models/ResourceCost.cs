namespace AICloudCostOptimizationAdvisor.Shared.Models;

/// <summary>
/// Represents the cost of a single Terraform resource.
/// </summary>
public class ResourceCost
{
    /// <summary>
    /// Resource identifier from Terraform.
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Resource type (e.g., aws_instance, azurerm_virtual_machine, google_compute_instance).
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Cloud provider (AWS, Azure, GCP).
    /// </summary>
    public string CloudProvider { get; set; } = string.Empty;

    /// <summary>
    /// Region where the resource is deployed.
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Estimated monthly cost.
    /// </summary>
    public decimal MonthlyCost { get; set; }

    /// <summary>
    /// Estimated annual cost.
    /// </summary>
    public decimal AnnualCost { get; set; }

    /// <summary>
    /// Resource configuration details (instance type, size, etc.).
    /// </summary>
    public Dictionary<string, string> Configuration { get; set; } = new();

    /// <summary>
    /// Pricing breakdown details.
    /// </summary>
    public string? PricingDetails { get; set; }
}
