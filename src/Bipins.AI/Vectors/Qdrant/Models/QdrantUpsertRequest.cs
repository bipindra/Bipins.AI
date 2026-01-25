using System.Text.Json.Serialization;

namespace Bipins.AI.Vectors.Qdrant.Models;

/// <summary>
/// Internal DTO for Qdrant upsert request.
/// </summary>
internal record QdrantUpsertRequest(
    [property: JsonPropertyName("points")] IReadOnlyList<QdrantPoint> Points);

