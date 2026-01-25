using System.Text.Json.Serialization;

namespace Bipins.AI.Vectors.Qdrant.Models;

/// <summary>
/// Internal DTO for Qdrant create collection request.
/// </summary>
internal record QdrantCreateCollectionRequest(
    [property: JsonPropertyName("vectors")] QdrantVectorsConfig Vectors);

