using Bipins.AI.Agents;
using Bipins.AI.AgentSamples.Core;
using Bipins.AI.AgentSamples.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bipins.AI.AgentSamples.Scenarios;

/// <summary>
/// Scenario 3: Agent with memory across multiple requests.
/// Follows Single Responsibility Principle - only handles memory demonstration.
/// </summary>
public class MemoryScenario : ScenarioBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryScenario"/> class.
    /// </summary>
    public MemoryScenario(
        IAgent agent,
        IOutputFormatter outputFormatter,
        ILogger<MemoryScenario> logger)
        : base(agent, outputFormatter, logger)
    {
    }

    /// <inheritdoc />
    public override int Number => 3;

    /// <inheritdoc />
    public override string Name => "Agent with Memory";

    /// <inheritdoc />
    public override string Description => "Conversation context across multiple requests";

    /// <inheritdoc />
    protected override string GetGoal() => "Multiple requests in same session to demonstrate memory";

    /// <inheritdoc />
    protected override async Task ExecuteScenarioAsync(Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        var sessionId = $"session-{Guid.NewGuid():N}";
        Console.WriteLine($"Session ID: {sessionId}");
        Console.WriteLine();

        // First request
        Console.WriteLine("💬 Request 1: What's the weather in New York?");
        var request1 = new AgentRequest(
            Goal: "What's the weather in New York?",
            SessionId: sessionId);

        var response1 = await Agent.ExecuteAsync(request1, cancellationToken);
        Console.WriteLine($"✅ Response 1: {response1.Content}");
        Console.WriteLine();

        // Second request - should remember previous context
        Console.WriteLine("💬 Request 2: What about Los Angeles?");
        var request2 = new AgentRequest(
            Goal: "What about Los Angeles?",
            SessionId: sessionId);

        var response2 = await Agent.ExecuteAsync(request2, cancellationToken);
        Console.WriteLine($"✅ Response 2: {response2.Content}");
        Console.WriteLine();

        // Third request - should understand context
        Console.WriteLine("💬 Request 3: Compare the two cities");
        var request3 = new AgentRequest(
            Goal: "Compare the weather in those two cities",
            SessionId: sessionId);

        var response3 = await Agent.ExecuteAsync(request3, cancellationToken);
        stopwatch.Stop();

        Console.WriteLine($"✅ Response 3: {response3.Content}");
        Console.WriteLine();
        Console.WriteLine("📊 Execution Details:");
        Console.WriteLine($"   Status: {response3.Status}");
        Console.WriteLine($"   Iterations: {response3.Iterations}");
        Console.WriteLine($"   Total Duration: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"   💡 Note: Agent remembered previous conversation context!");
        OutputFormatter.WriteSeparator();
    }
}
