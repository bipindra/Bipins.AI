using Bipins.AI.Agents;

namespace Bipins.AI.AgentSamples.Services;

/// <summary>
/// Console-based output formatter.
/// Follows Single Responsibility Principle - only handles output formatting.
/// </summary>
public class ConsoleOutputFormatter : IOutputFormatter
{
    /// <inheritdoc />
    public void WriteWelcomeBanner()
    {
        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         Bipins.AI Agent Samples - Interactive Demo         ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
    }

    /// <inheritdoc />
    public void WriteAgentInfo(IAgent agent)
    {
        Console.WriteLine("📋 Agent Information:");
        Console.WriteLine($"   ID: {agent.Id}");
        Console.WriteLine($"   Name: {agent.Name}");
        Console.WriteLine($"   Capabilities: {agent.Capabilities}");
        Console.WriteLine();
    }

    /// <inheritdoc />
    public void WriteScenarioHeader(int number, string name)
    {
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine($"📌 Scenario {number}: {name}");
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    /// <inheritdoc />
    public void WriteGoal(string goal)
    {
        Console.WriteLine($"Goal: {goal}");
        Console.WriteLine();
    }

    /// <inheritdoc />
    public void WriteExecuting()
    {
        Console.WriteLine("⏳ Executing agent...");
    }

    /// <inheritdoc />
    public void WriteResponse(string content)
    {
        Console.WriteLine();
        Console.WriteLine("✅ Agent Response:");
        Console.WriteLine($"   {content}");
        Console.WriteLine();
    }

    /// <inheritdoc />
    public void WriteExecutionDetails(AgentResponse response, long durationMs)
    {
        Console.WriteLine("📊 Execution Details:");
        Console.WriteLine($"   Status: {response.Status}");
        Console.WriteLine($"   Iterations: {response.Iterations}");
        Console.WriteLine($"   Duration: {durationMs}ms");
    }

    /// <inheritdoc />
    public void WriteToolCalls(IReadOnlyList<Bipins.AI.Core.Models.ToolCall>? toolCalls)
    {
        if (toolCalls == null || toolCalls.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"🔧 Tool Calls Made: {toolCalls.Count}");
        foreach (var toolCall in toolCalls)
        {
            Console.WriteLine($"   • {toolCall.Name}");
            Console.WriteLine($"     Arguments: {toolCall.Arguments}");
        }
    }

    /// <inheritdoc />
    public void WriteExecutionPlan(AgentExecutionPlan? plan)
    {
        if (plan == null)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("📋 Execution Plan:");
        Console.WriteLine($"   Goal: {plan.Goal}");
        if (!string.IsNullOrEmpty(plan.Reasoning))
        {
            Console.WriteLine($"   Reasoning: {plan.Reasoning}");
        }
        Console.WriteLine($"   Steps: {plan.Steps.Count}");
        foreach (var step in plan.Steps)
        {
            Console.WriteLine($"   {step.Order}. {step.Action}");
            if (!string.IsNullOrEmpty(step.ToolName))
            {
                Console.WriteLine($"      🔧 Tool: {step.ToolName}");
            }
            if (!string.IsNullOrEmpty(step.ExpectedOutcome))
            {
                Console.WriteLine($"      📌 Expected: {step.ExpectedOutcome}");
            }
        }
    }

    /// <inheritdoc />
    public void WriteError(string message)
    {
        Console.WriteLine($"❌ {message}");
    }

    /// <inheritdoc />
    public void WriteWarning(string message)
    {
        Console.WriteLine($"⚠️  {message}");
    }

    /// <inheritdoc />
    public void WriteSuccess(string message)
    {
        Console.WriteLine($"✅ {message}");
    }

    /// <inheritdoc />
    public void WriteSeparator()
    {
        Console.WriteLine();
    }
}
