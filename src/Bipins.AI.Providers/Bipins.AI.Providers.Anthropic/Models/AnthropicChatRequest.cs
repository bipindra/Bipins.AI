using System.Text.Json.Serialization;

namespace Bipins.AI.Providers.Anthropic.Models;

/// <summary>
/// Internal DTO for Anthropic chat request.
/// </summary>
internal record AnthropicChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("max_tokens")] int MaxTokens,
    [property: JsonPropertyName("messages")] List<AnthropicMessage> Messages,
    [property: JsonPropertyName("system")] string? System = null,
    [property: JsonPropertyName("temperature")] float? Temperature = null,
    [property: JsonPropertyName("tools")] List<AnthropicTool>? Tools = null);

