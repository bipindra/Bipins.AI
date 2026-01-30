using Bipins.AI.Agents;

namespace Bipins.AI.AgentSamples.Services;

/// <summary>
/// Interface for formatting output to the console.
/// Follows Interface Segregation Principle - focused interface for output formatting.
/// </summary>
public interface IOutputFormatter
{
    /// <summary>
    /// Writes the welcome banner.
    /// </summary>
    void WriteWelcomeBanner();

    /// <summary>
    /// Writes agent information.
    /// </summary>
    void WriteAgentInfo(IAgent agent);

    /// <summary>
    /// Writes scenario header.
    /// </summary>
    void WriteScenarioHeader(int number, string name);

    /// <summary>
    /// Writes the goal description.
    /// </summary>
    void WriteGoal(string goal);

    /// <summary>
    /// Writes executing message.
    /// </summary>
    void WriteExecuting();

    /// <summary>
    /// Writes agent response.
    /// </summary>
    void WriteResponse(string content);

    /// <summary>
    /// Writes execution details.
    /// </summary>
    void WriteExecutionDetails(AgentResponse response, long durationMs);

    /// <summary>
    /// Writes tool calls information.
    /// </summary>
    void WriteToolCalls(IReadOnlyList<Bipins.AI.Core.Models.ToolCall>? toolCalls);

    /// <summary>
    /// Writes execution plan.
    /// </summary>
    void WriteExecutionPlan(AgentExecutionPlan? plan);

    /// <summary>
    /// Writes an error message.
    /// </summary>
    void WriteError(string message);

    /// <summary>
    /// Writes a warning message.
    /// </summary>
    void WriteWarning(string message);

    /// <summary>
    /// Writes a success message.
    /// </summary>
    void WriteSuccess(string message);

    /// <summary>
    /// Writes a separator line.
    /// </summary>
    void WriteSeparator();
}
