using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Llm.OpenAI.Models;

/// <summary>
/// Internal DTO for OpenAI choice.
/// </summary>
internal record OpenAiChoice(
    [property: JsonPropertyName("message")] OpenAiChatMessage? Message,
    [property: JsonPropertyName("finish_reason")] string? FinishReason);
