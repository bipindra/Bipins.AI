using System.Text.Json.Serialization;

namespace Bipins.AI.Vectors.Qdrant.Models;

/// <summary>
/// Internal DTO for Qdrant collection params.
/// </summary>
internal record QdrantCollectionParams(
    [property: JsonPropertyName("vectors")] QdrantVectorsConfig? Vectors);

