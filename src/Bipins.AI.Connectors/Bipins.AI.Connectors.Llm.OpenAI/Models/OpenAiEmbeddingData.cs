using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Llm.OpenAI.Models;

/// <summary>
/// Internal DTO for OpenAI embedding data.
/// </summary>
internal record OpenAiEmbeddingData(
    [property: JsonPropertyName("embedding")] float[] Embedding,
    [property: JsonPropertyName("index")] int Index);
