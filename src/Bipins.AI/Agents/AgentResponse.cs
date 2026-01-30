using Bipins.AI.Core.Models;

namespace Bipins.AI.Agents;

/// <summary>
/// Response from agent execution.
/// </summary>
/// <param name="Content">The generated content or response.</param>
/// <param name="Status">Current status of the agent execution.</param>
/// <param name="ToolCalls">List of tool calls made during execution.</param>
/// <param name="Plan">Optional execution plan if planning was enabled.</param>
/// <param name="Iterations">Number of iterations taken to complete.</param>
/// <param name="Metadata">Additional metadata about the execution.</param>
public record AgentResponse(
    string Content,
    AgentStatus Status,
    IReadOnlyList<ToolCall>? ToolCalls = null,
    AgentExecutionPlan? Plan = null,
    int Iterations = 0,
    Dictionary<string, object>? Metadata = null);

/// <summary>
/// Status of agent execution.
/// </summary>
public enum AgentStatus
{
    Planning,
    Executing,
    WaitingForTool,
    Completed,
    Failed,
    Cancelled,
    MaxIterationsReached
}

/// <summary>
/// Streaming chunk of agent response.
/// </summary>
/// <param name="Content">Content chunk.</param>
/// <param name="Status">Current status.</param>
/// <param name="IsComplete">Whether this is the final chunk.</param>
/// <param name="ToolCalls">Tool calls if any.</param>
public record AgentResponseChunk(
    string Content,
    AgentStatus Status,
    bool IsComplete = false,
    IReadOnlyList<ToolCall>? ToolCalls = null);
