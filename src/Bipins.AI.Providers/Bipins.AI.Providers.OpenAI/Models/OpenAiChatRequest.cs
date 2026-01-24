using System.Text.Json.Serialization;

namespace Bipins.AI.Providers.OpenAI.Models;

/// <summary>
/// Internal DTO for OpenAI chat request.
/// </summary>
internal record OpenAiChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] IReadOnlyList<OpenAiChatMessage> Messages,
    [property: JsonPropertyName("temperature")] float? Temperature = null,
    [property: JsonPropertyName("max_tokens")] int? MaxTokens = null,
    [property: JsonPropertyName("tools")] IReadOnlyList<OpenAiTool>? Tools = null,
    [property: JsonPropertyName("tool_choice")] object? ToolChoice = null,
    [property: JsonPropertyName("response_format")] object? ResponseFormat = null);

