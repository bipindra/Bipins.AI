using System.Text.Json.Serialization;

namespace Bipins.AI.Providers.Bedrock.Models;

/// <summary>
/// Internal DTOs for AWS Bedrock.
/// </summary>
internal record BedrockMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] List<BedrockContentBlock> Content);

internal record BedrockContentBlock(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string? Text = null,
    [property: JsonPropertyName("id")] string? Id = null,
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("input")] object? Input = null);

internal record BedrockChatRequest(
    [property: JsonPropertyName("anthropic_version")] string AnthropicVersion,
    [property: JsonPropertyName("max_tokens")] int MaxTokens,
    [property: JsonPropertyName("messages")] List<BedrockMessage> Messages,
    [property: JsonPropertyName("system")] string? System = null,
    [property: JsonPropertyName("temperature")] float? Temperature = null,
    [property: JsonPropertyName("tools")] List<BedrockTool>? Tools = null);

internal record BedrockChatResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] List<BedrockContentBlock> Content,
    [property: JsonPropertyName("stop_reason")] string? StopReason,
    [property: JsonPropertyName("stop_sequence")] string? StopSequence,
    [property: JsonPropertyName("usage")] BedrockUsage? Usage);

internal record BedrockUsage(
    [property: JsonPropertyName("input_tokens")] int InputTokens,
    [property: JsonPropertyName("output_tokens")] int OutputTokens);

/// <summary>
/// Internal DTO for Bedrock tool definition.
/// </summary>
internal record BedrockTool(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("input_schema")] object InputSchema);

