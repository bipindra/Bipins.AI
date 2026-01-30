using Bipins.AI.Core.Models;

namespace Bipins.AI.Agents;

/// <summary>
/// Request for agent execution.
/// </summary>
/// <param name="Goal">The goal or task for the agent to accomplish.</param>
/// <param name="Context">Optional context or background information.</param>
/// <param name="Parameters">Optional parameters for the request.</param>
/// <param name="AvailableTools">Optional list of available tools. If null, agent uses all registered tools.</param>
/// <param name="SessionId">Optional session ID for maintaining conversation context.</param>
/// <param name="Metadata">Additional metadata for the request.</param>
public record AgentRequest(
    string Goal,
    string? Context = null,
    Dictionary<string, object>? Parameters = null,
    IReadOnlyList<ToolDefinition>? AvailableTools = null,
    string? SessionId = null,
    Dictionary<string, object>? Metadata = null);
