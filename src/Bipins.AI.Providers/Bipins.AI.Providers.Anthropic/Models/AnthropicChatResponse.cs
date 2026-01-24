using System.Text.Json.Serialization;

namespace Bipins.AI.Providers.Anthropic.Models;

/// <summary>
/// Internal DTO for Anthropic chat response.
/// </summary>
internal record AnthropicChatResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] List<AnthropicContentBlock> Content,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("stop_reason")] string? StopReason,
    [property: JsonPropertyName("stop_sequence")] string? StopSequence,
    [property: JsonPropertyName("usage")] AnthropicUsage? Usage);

/// <summary>
/// Content block in Anthropic response.
/// </summary>
internal record AnthropicContentBlock(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string? Text = null,
    [property: JsonPropertyName("id")] string? Id = null,
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("input")] object? Input = null);

/// <summary>
/// Usage information from Anthropic.
/// </summary>
internal record AnthropicUsage(
    [property: JsonPropertyName("input_tokens")] int InputTokens,
    [property: JsonPropertyName("output_tokens")] int OutputTokens);

