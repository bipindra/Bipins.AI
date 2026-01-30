using Bipins.AI.Agents;
using Bipins.AI.AgentSamples.Core;
using Bipins.AI.AgentSamples.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bipins.AI.AgentSamples.Scenarios;

/// <summary>
/// Scenario 1: Basic agent execution with calculator tool.
/// Follows Single Responsibility Principle - only handles basic agent execution demonstration.
/// </summary>
public class BasicAgentScenario : ScenarioBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BasicAgentScenario"/> class.
    /// </summary>
    public BasicAgentScenario(
        IAgent agent,
        IOutputFormatter outputFormatter,
        ILogger<BasicAgentScenario> logger)
        : base(agent, outputFormatter, logger)
    {
    }

    /// <inheritdoc />
    public override int Number => 1;

    /// <inheritdoc />
    public override string Name => "Basic Agent Execution";

    /// <inheritdoc />
    public override string Description => "Simple agent with calculator tool";

    /// <inheritdoc />
    protected override string GetGoal() => "Calculate 15 * 23 + 42";

    /// <inheritdoc />
    protected override async Task ExecuteScenarioAsync(Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        var request = new AgentRequest(
            Goal: "Calculate 15 * 23 + 42",
            Context: "User wants a mathematical calculation");

        var response = await Agent.ExecuteAsync(request, cancellationToken);
        stopwatch.Stop();

        OutputFormatter.WriteResponse(response.Content);
        OutputFormatter.WriteExecutionDetails(response, stopwatch.ElapsedMilliseconds);
        OutputFormatter.WriteToolCalls(response.ToolCalls);
        OutputFormatter.WriteSeparator();
    }
}
