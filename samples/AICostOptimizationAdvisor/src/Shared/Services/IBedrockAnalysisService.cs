using AICostOptimizationAdvisor.Shared.Models;

namespace AICostOptimizationAdvisor.Shared.Services;

/// <summary>
/// Service interface for analyzing cost data using AWS Bedrock.
/// </summary>
public interface IBedrockAnalysisService
{
    /// <summary>
    /// Analyzes cost data using Bedrock AI and returns optimization suggestions.
    /// </summary>
    Task<CostAnalysis> AnalyzeCostsAsync(GetCostDataResponse costData, string? modelId = null, CancellationToken cancellationToken = default);
}
