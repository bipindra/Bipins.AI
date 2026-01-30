using Bipins.AI.Agents;
using Bipins.AI.AgentSamples.Core;
using Bipins.AI.AgentSamples.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bipins.AI.AgentSamples.Scenarios;

/// <summary>
/// Scenario 2: Agent using multiple tools in sequence.
/// Follows Single Responsibility Principle - only handles multiple tools demonstration.
/// </summary>
public class MultipleToolsScenario : ScenarioBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultipleToolsScenario"/> class.
    /// </summary>
    public MultipleToolsScenario(
        IAgent agent,
        IOutputFormatter outputFormatter,
        ILogger<MultipleToolsScenario> logger)
        : base(agent, outputFormatter, logger)
    {
    }

    /// <inheritdoc />
    public override int Number => 2;

    /// <inheritdoc />
    public override string Name => "Agent with Multiple Tools";

    /// <inheritdoc />
    public override string Description => "Agent using multiple tools in sequence";

    /// <inheritdoc />
    protected override string GetGoal() => "Get weather in San Francisco and convert 72°F to Celsius";

    /// <inheritdoc />
    protected override async Task ExecuteScenarioAsync(Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        var request = new AgentRequest(
            Goal: "What's the weather in San Francisco? Also, if it's 72 degrees Fahrenheit, what is that in Celsius?",
            Context: "User wants weather information and temperature conversion");

        var response = await Agent.ExecuteAsync(request, cancellationToken);
        stopwatch.Stop();

        OutputFormatter.WriteResponse(response.Content);
        OutputFormatter.WriteExecutionDetails(response, stopwatch.ElapsedMilliseconds);
        OutputFormatter.WriteToolCalls(response.ToolCalls);
        OutputFormatter.WriteSeparator();
    }
}
