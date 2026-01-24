using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Vector.Qdrant.Models;

/// <summary>
/// Internal DTO for Qdrant search request.
/// </summary>
internal record QdrantSearchRequest(
    [property: JsonPropertyName("vector")] float[] Vector,
    [property: JsonPropertyName("limit")] int Limit,
    [property: JsonPropertyName("filter")] JsonElement? Filter = null,
    [property: JsonPropertyName("with_payload")] bool WithPayload = true,
    [property: JsonPropertyName("with_vector")] bool WithVector = false);
