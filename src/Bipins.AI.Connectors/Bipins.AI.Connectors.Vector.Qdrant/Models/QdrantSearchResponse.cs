using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Vector.Qdrant.Models;

/// <summary>
/// Internal DTO for Qdrant search response.
/// </summary>
internal record QdrantSearchResponse(
    [property: JsonPropertyName("result")] IReadOnlyList<QdrantScoredPoint> Result);
