using System.Text.Json.Serialization;

namespace Bipins.AI.Providers.OpenAI.Models;

/// <summary>
/// Internal DTO for OpenAI chat response.
/// </summary>
internal record OpenAiChatResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("choices")] IReadOnlyList<OpenAiChoice> Choices,
    [property: JsonPropertyName("usage")] OpenAiUsage? Usage,
    [property: JsonPropertyName("model")] string Model);

