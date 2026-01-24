using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Vector.Qdrant.Models;

/// <summary>
/// Internal DTO for Qdrant scored point.
/// </summary>
internal record QdrantScoredPoint(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("score")] float Score,
    [property: JsonPropertyName("payload")] Dictionary<string, object>? Payload = null);
