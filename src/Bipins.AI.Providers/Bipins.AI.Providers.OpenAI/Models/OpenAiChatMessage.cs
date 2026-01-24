using System.Text.Json.Serialization;

namespace Bipins.AI.Providers.OpenAI.Models;

/// <summary>
/// Internal DTO for OpenAI chat message.
/// </summary>
internal record OpenAiChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string? Content = null,
    [property: JsonPropertyName("tool_call_id")] string? ToolCallId = null,
    [property: JsonPropertyName("tool_calls")] IReadOnlyList<OpenAiToolCall>? ToolCalls = null);

