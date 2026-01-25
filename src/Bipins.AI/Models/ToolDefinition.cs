using System.Text.Json;

namespace Bipins.AI.Core.Models;

/// <summary>
/// Definition of a tool/function available to the model.
/// </summary>
/// <param name="Name">Name of the tool.</param>
/// <param name="Description">Description of what the tool does.</param>
/// <param name="Parameters">JSON schema for the tool parameters.</param>
public record ToolDefinition(
    string Name,
    string Description,
    JsonElement Parameters);
