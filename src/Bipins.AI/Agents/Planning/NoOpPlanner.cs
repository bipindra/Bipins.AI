using Bipins.AI.Agents.Memory;
using Bipins.AI.Core.Models;
using PlanStep = Bipins.AI.Agents.PlanStep;

namespace Bipins.AI.Agents.Planning;

/// <summary>
/// No-op planner that returns a simple single-step plan.
/// </summary>
public class NoOpPlanner : IAgentPlanner
{
    /// <inheritdoc />
    public Task<AgentExecutionPlan> CreatePlanAsync(
        AgentRequest request,
        IReadOnlyList<ToolDefinition> availableTools,
        AgentMemoryContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var plan = new AgentExecutionPlan(
            request.Goal,
            new List<PlanStep> { new PlanStep(1, $"Execute: {request.Goal}") },
            "No-op plan: execute goal directly");

        return Task.FromResult(plan);
    }
}
