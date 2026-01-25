using AICloudCostOptimizationAdvisor.Shared.Models;

namespace AICloudCostOptimizationAdvisor.Shared.Services;

/// <summary>
/// Service interface for AI-powered cost analysis and optimization suggestions.
/// </summary>
public interface IAICostAnalysisService
{
    /// <summary>
    /// Analyzes Terraform resources and costs to generate optimization suggestions.
    /// </summary>
    Task<TerraformAnalysis> AnalyzeAsync(
        TerraformAnalysis analysis,
        string? modelId = null,
        bool includeSecurityRisks = false,
        bool includeMermaidDiagrams = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes and streams optimization suggestions.
    /// </summary>
    IAsyncEnumerable<TerraformAnalysis> AnalyzeStreamAsync(
        TerraformAnalysis analysis,
        string? modelId = null,
        CancellationToken cancellationToken = default);
}
