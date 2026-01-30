using Bipins.AI.Agents;
using Bipins.AI.AgentSamples.Core;
using Bipins.AI.AgentSamples.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bipins.AI.AgentSamples.Scenarios;

/// <summary>
/// Scenario 4: Agent with planning for complex multi-step tasks.
/// Follows Single Responsibility Principle - only handles planning demonstration.
/// </summary>
public class PlanningScenario : ScenarioBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlanningScenario"/> class.
    /// </summary>
    public PlanningScenario(
        IAgent agent,
        IOutputFormatter outputFormatter,
        ILogger<PlanningScenario> logger)
        : base(agent, outputFormatter, logger)
    {
    }

    /// <inheritdoc />
    public override int Number => 4;

    /// <inheritdoc />
    public override string Name => "Agent with Planning";

    /// <inheritdoc />
    public override string Description => "Complex multi-step tasks with execution planning";

    /// <inheritdoc />
    protected override string GetGoal() => "Complex multi-step task requiring planning";

    /// <inheritdoc />
    protected override async Task ExecuteScenarioAsync(Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        var request = new AgentRequest(
            Goal: "Get the weather in Seattle, then calculate what that temperature would be in Celsius, and finally tell me if it's a good day for outdoor activities",
            Context: "User wants a comprehensive weather analysis with recommendations");

        var response = await Agent.ExecuteAsync(request, cancellationToken);
        stopwatch.Stop();

        OutputFormatter.WriteResponse(response.Content);
        OutputFormatter.WriteExecutionDetails(response, stopwatch.ElapsedMilliseconds);
        OutputFormatter.WriteExecutionPlan(response.Plan);
        OutputFormatter.WriteToolCalls(response.ToolCalls);
        OutputFormatter.WriteSeparator();
    }
}
