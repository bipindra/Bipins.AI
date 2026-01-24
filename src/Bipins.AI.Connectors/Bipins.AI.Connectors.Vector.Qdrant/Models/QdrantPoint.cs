using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Vector.Qdrant.Models;

/// <summary>
/// Internal DTO for Qdrant point.
/// </summary>
internal record QdrantPoint(
    string Id,
    float[] Vector,
    [property: JsonPropertyName("payload")] Dictionary<string, object>? Payload = null);
