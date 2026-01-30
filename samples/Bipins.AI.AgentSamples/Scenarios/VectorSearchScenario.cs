using Bipins.AI.Agents;
using Bipins.AI.AgentSamples.Core;
using Bipins.AI.AgentSamples.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bipins.AI.AgentSamples.Scenarios;

/// <summary>
/// Scenario 6: Agent with vector search (RAG integration).
/// Follows Single Responsibility Principle - only handles vector search demonstration.
/// </summary>
public class VectorSearchScenario : ScenarioBase
{
    private readonly Bipins.AI.Vector.IVectorStore? _vectorStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="VectorSearchScenario"/> class.
    /// </summary>
    public VectorSearchScenario(
        IAgent agent,
        IOutputFormatter outputFormatter,
        ILogger<VectorSearchScenario> logger,
        Bipins.AI.Vector.IVectorStore? vectorStore = null)
        : base(agent, outputFormatter, logger)
    {
        _vectorStore = vectorStore;
    }

    /// <inheritdoc />
    public override int Number => 6;

    /// <inheritdoc />
    public override string Name => "Agent with Vector Search";

    /// <inheritdoc />
    public override string Description => "RAG integration with vector search tool";

    /// <inheritdoc />
    public override bool RequiresVectorStore => true;

    /// <inheritdoc />
    protected override string GetGoal() => "Search knowledge base and answer question";

    /// <inheritdoc />
    protected override async Task ExecuteScenarioAsync(Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        if (_vectorStore == null)
        {
            OutputFormatter.WriteWarning("Scenario 6: Agent with Vector Search - SKIPPED");
            Console.WriteLine("   Qdrant not configured. Set QDRANT_ENDPOINT to enable this scenario.");
            OutputFormatter.WriteSeparator();
            return;
        }

        Console.WriteLine("Note: This scenario requires a vector store with indexed documents");
        Console.WriteLine();

        var request = new AgentRequest(
            Goal: "Search the knowledge base for information about machine learning and summarize what you find",
            Context: "User wants to search documents and get a summary");

        var response = await Agent.ExecuteAsync(request, cancellationToken);
        stopwatch.Stop();

        OutputFormatter.WriteResponse(response.Content);
        OutputFormatter.WriteExecutionDetails(response, stopwatch.ElapsedMilliseconds);
        OutputFormatter.WriteToolCalls(response.ToolCalls);
        OutputFormatter.WriteSeparator();
    }
}
