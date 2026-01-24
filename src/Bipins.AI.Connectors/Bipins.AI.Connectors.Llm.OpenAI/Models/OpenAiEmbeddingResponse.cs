using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Llm.OpenAI.Models;

/// <summary>
/// Internal DTO for OpenAI embedding response.
/// </summary>
internal record OpenAiEmbeddingResponse(
    [property: JsonPropertyName("data")] IReadOnlyList<OpenAiEmbeddingData> Data,
    [property: JsonPropertyName("usage")] OpenAiUsage? Usage,
    [property: JsonPropertyName("model")] string Model);
