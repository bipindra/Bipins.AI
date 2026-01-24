using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Llm.OpenAI.Models;

/// <summary>
/// Internal DTO for OpenAI chat message.
/// </summary>
internal record OpenAiChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("tool_call_id")] string? ToolCallId = null);
