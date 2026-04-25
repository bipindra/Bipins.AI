using Bipins.AI.Agents.Memory;
using Bipins.AI.Core.Models;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Agents.Planning;

/// <summary>
/// Semantic Kernel-enabled planner facade.
/// Uses existing LLM planner behavior but provides a dedicated registration target.
/// </summary>
public sealed class SemanticKernelPlanner : IAgentPlanner
{
    private readonly LLMPlanner _innerPlanner;
    private readonly ILogger<SemanticKernelPlanner>? _logger;

    public SemanticKernelPlanner(LLMPlanner innerPlanner, ILogger<SemanticKernelPlanner>? logger = null)
    {
        _innerPlanner = innerPlanner;
        _logger = logger;
    }

    public async Task<AgentExecutionPlan> CreatePlanAsync(
        AgentRequest request,
        IReadOnlyList<ToolDefinition> availableTools,
        AgentMemoryContext? context = null,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Using SemanticKernelPlanner facade for goal '{Goal}'", request.Goal);
        return await _innerPlanner.CreatePlanAsync(request, availableTools, context, cancellationToken);
    }
}
