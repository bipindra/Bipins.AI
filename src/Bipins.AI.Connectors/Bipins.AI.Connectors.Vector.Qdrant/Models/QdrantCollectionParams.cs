using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Vector.Qdrant.Models;

/// <summary>
/// Internal DTO for Qdrant collection params.
/// </summary>
internal record QdrantCollectionParams(
    [property: JsonPropertyName("vectors")] QdrantVectorsConfig? Vectors);
