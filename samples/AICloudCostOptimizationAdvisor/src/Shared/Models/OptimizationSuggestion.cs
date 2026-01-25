namespace AICloudCostOptimizationAdvisor.Shared.Models;

/// <summary>
/// AI-generated optimization suggestion.
/// </summary>
public class OptimizationSuggestion
{
    /// <summary>
    /// Category of optimization (Compute, Storage, Network, Other).
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Description of the optimization opportunity.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Estimated savings (e.g., "$50-100/month").
    /// </summary>
    public string EstimatedSavings { get; set; } = string.Empty;

    /// <summary>
    /// Priority level (High, Medium, Low).
    /// </summary>
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Specific actions to take.
    /// </summary>
    public List<string> Actions { get; set; } = new();

    /// <summary>
    /// Related resource IDs or services.
    /// </summary>
    public List<string> RelatedResources { get; set; } = new();

    /// <summary>
    /// Cloud provider this suggestion applies to.
    /// </summary>
    public string CloudProvider { get; set; } = string.Empty;
}
