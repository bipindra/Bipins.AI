using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Llm.AzureOpenAI.Models;

/// <summary>
/// Internal DTOs for Azure OpenAI (same format as OpenAI).
/// </summary>
internal record AzureOpenAiChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string? Content = null,
    [property: JsonPropertyName("tool_call_id")] string? ToolCallId = null,
    [property: JsonPropertyName("tool_calls")] IReadOnlyList<AzureOpenAiToolCall>? ToolCalls = null);

internal record AzureOpenAiChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] IReadOnlyList<AzureOpenAiChatMessage> Messages,
    [property: JsonPropertyName("temperature")] float? Temperature = null,
    [property: JsonPropertyName("max_tokens")] int? MaxTokens = null,
    [property: JsonPropertyName("tools")] IReadOnlyList<AzureOpenAiTool>? Tools = null,
    [property: JsonPropertyName("tool_choice")] object? ToolChoice = null);

internal record AzureOpenAiTool(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("function")] AzureOpenAiFunction Function);

internal record AzureOpenAiFunction(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("parameters")] object Parameters);

internal record AzureOpenAiToolCall(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("function")] AzureOpenAiFunctionCall Function);

internal record AzureOpenAiFunctionCall(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("arguments")] string Arguments);

internal record AzureOpenAiChatResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("choices")] IReadOnlyList<AzureOpenAiChoice> Choices,
    [property: JsonPropertyName("usage")] AzureOpenAiUsage? Usage,
    [property: JsonPropertyName("model")] string Model);

internal record AzureOpenAiChoice(
    [property: JsonPropertyName("message")] AzureOpenAiChatMessage? Message,
    [property: JsonPropertyName("finish_reason")] string? FinishReason);

internal record AzureOpenAiUsage(
    [property: JsonPropertyName("prompt_tokens")] int PromptTokens,
    [property: JsonPropertyName("completion_tokens")] int CompletionTokens,
    [property: JsonPropertyName("total_tokens")] int TotalTokens);

internal record AzureOpenAiEmbeddingRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("input")] List<string> Input);

internal record AzureOpenAiEmbeddingResponse(
    [property: JsonPropertyName("data")] IReadOnlyList<AzureOpenAiEmbeddingData> Data,
    [property: JsonPropertyName("usage")] AzureOpenAiUsage? Usage,
    [property: JsonPropertyName("model")] string Model);

internal record AzureOpenAiEmbeddingData(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("embedding")] List<float> Embedding);
