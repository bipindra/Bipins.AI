using System.Text.Json;

namespace Bipins.AI.Core.Models;

/// <summary>
/// Represents a tool/function call from the model.
/// </summary>
/// <param name="Id">Unique identifier for the tool call.</param>
/// <param name="Name">Name of the tool/function to call.</param>
/// <param name="Arguments">JSON arguments for the tool call.</param>
public record ToolCall(
    string Id,
    string Name,
    JsonElement Arguments);
