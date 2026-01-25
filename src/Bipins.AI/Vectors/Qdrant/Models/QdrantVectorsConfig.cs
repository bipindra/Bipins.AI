using System.Text.Json.Serialization;

namespace Bipins.AI.Vectors.Qdrant.Models;

/// <summary>
/// Internal DTO for Qdrant vectors config.
/// </summary>
internal record QdrantVectorsConfig(
    [property: JsonPropertyName("size")] int Size,
    [property: JsonPropertyName("distance")] string Distance);

