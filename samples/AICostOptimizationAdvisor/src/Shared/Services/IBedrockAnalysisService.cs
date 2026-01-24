using AICostOptimizationAdvisor.Shared.Models;
using Bipins.AI.Core.Models;

namespace AICostOptimizationAdvisor.Shared.Services;

/// <summary>
/// Service interface for analyzing cost data using Bipins.AI platform-agnostic interfaces.
/// </summary>
public interface IBedrockAnalysisService
{
    /// <summary>
    /// Analyzes cost data using AI and returns optimization suggestions.
    /// </summary>
    Task<CostAnalysis> AnalyzeCostsAsync(GetCostDataResponse costData, string? modelId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes cost data using streaming AI and returns optimization suggestions as they are generated.
    /// </summary>
    IAsyncEnumerable<CostAnalysis> AnalyzeCostsStreamAsync(GetCostDataResponse costData, string? modelId = null, CancellationToken cancellationToken = default);
}
