using System.Text.Json.Serialization;

namespace Bipins.AI.Providers.OpenAI.Models;

/// <summary>
/// Internal DTO for OpenAI embedding request.
/// </summary>
internal record OpenAiEmbeddingRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("input")] IReadOnlyList<string> Input);

