using Bipins.AI.Agents;
using Bipins.AI.AgentSamples.Core;
using Bipins.AI.AgentSamples.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bipins.AI.AgentSamples.Scenarios;

/// <summary>
/// Scenario 5: Streaming agent execution.
/// Follows Single Responsibility Principle - only handles streaming demonstration.
/// </summary>
public class StreamingScenario : ScenarioBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingScenario"/> class.
    /// </summary>
    public StreamingScenario(
        IAgent agent,
        IOutputFormatter outputFormatter,
        ILogger<StreamingScenario> logger)
        : base(agent, outputFormatter, logger)
    {
    }

    /// <inheritdoc />
    public override int Number => 5;

    /// <inheritdoc />
    public override string Name => "Streaming Agent Execution";

    /// <inheritdoc />
    public override string Description => "Real-time streaming of agent responses";

    /// <inheritdoc />
    protected override string GetGoal() => "Calculate sqrt(144) + 25 and explain the steps";

    /// <inheritdoc />
    protected override async Task ExecuteScenarioAsync(Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        var request = new AgentRequest(
            Goal: "Calculate sqrt(144) + 25 and explain each step of the calculation",
            Context: "User wants a calculation with explanation");

        Console.WriteLine("⏳ Streaming response (real-time):");
        Console.WriteLine();
        Console.Write("   ");

        var accumulatedContent = new System.Text.StringBuilder();
        var chunkCount = 0;
        await foreach (var chunk in Agent.ExecuteStreamAsync(request, cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                accumulatedContent.Append(chunk.Content);
                Console.Write(chunk.Content);
                chunkCount++;
            }

            if (chunk.IsComplete)
            {
                stopwatch.Stop();
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("📊 Streaming Details:");
                Console.WriteLine($"   Status: {chunk.Status}");
                Console.WriteLine($"   Chunks Received: {chunkCount}");
                Console.WriteLine($"   Duration: {stopwatch.ElapsedMilliseconds}ms");
                if (chunk.ToolCalls != null && chunk.ToolCalls.Count > 0)
                {
                    Console.WriteLine($"   Tool Calls: {chunk.ToolCalls.Count}");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("✅ Final Content:");
        Console.WriteLine($"   {accumulatedContent.ToString()}");
        OutputFormatter.WriteSeparator();
    }
}
