using System.Text.Json;
using Bipins.AI.Core.Models;

namespace Bipins.AI.Agents.Tools;

/// <summary>
/// Interface for executing tools/functions that agents can use.
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Name of the tool (must match ToolDefinition.Name).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description of what the tool does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// JSON schema for the tool parameters.
    /// </summary>
    JsonElement ParametersSchema { get; }

    /// <summary>
    /// Executes the tool with the given tool call.
    /// </summary>
    Task<ToolExecutionResult> ExecuteAsync(ToolCall toolCall, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of tool execution.
/// </summary>
/// <param name="Success">Whether the execution was successful.</param>
/// <param name="Result">The result object (will be serialized to JSON).</param>
/// <param name="Error">Error message if execution failed.</param>
/// <param name="Metadata">Additional metadata about the execution.</param>
public record ToolExecutionResult(
    bool Success,
    object? Result = null,
    string? Error = null,
    Dictionary<string, object>? Metadata = null);
