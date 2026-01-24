namespace Bipins.AI.Core.Models;

/// <summary>
/// Request for chat completion.
/// </summary>
/// <param name="Messages">List of messages in the conversation.</param>
/// <param name="Tools">Optional list of available tools/functions.</param>
/// <param name="ToolChoice">Tool choice strategy (none, auto, or specific tool).</param>
/// <param name="Temperature">Sampling temperature (0-2).</param>
/// <param name="MaxTokens">Maximum tokens to generate.</param>
/// <param name="Metadata">Additional metadata.</param>
public record ChatRequest(
    IReadOnlyList<Message> Messages,
    IReadOnlyList<ToolDefinition>? Tools = null,
    string? ToolChoice = null,
    float? Temperature = null,
    int? MaxTokens = null,
    Dictionary<string, object>? Metadata = null);
