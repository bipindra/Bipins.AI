using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Llm.Anthropic.Models;

/// <summary>
/// Internal DTO for Anthropic message.
/// </summary>
internal record AnthropicMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);
