using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Vector.Qdrant.Models;

/// <summary>
/// Internal DTO for Qdrant delete request.
/// </summary>
internal record QdrantDeleteRequest(
    [property: JsonPropertyName("points")] IReadOnlyList<string> Points);
