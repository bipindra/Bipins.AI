using Bipins.AI.Agents.Memory;
using Bipins.AI.Core.Models;

namespace Bipins.AI.Agents.Planning;

/// <summary>
/// Interface for creating execution plans for agents.
/// </summary>
public interface IAgentPlanner
{
    /// <summary>
    /// Creates an execution plan for the given request.
    /// </summary>
    Task<AgentExecutionPlan> CreatePlanAsync(
        AgentRequest request,
        IReadOnlyList<ToolDefinition> availableTools,
        AgentMemoryContext? context = null,
        CancellationToken cancellationToken = default);
}
