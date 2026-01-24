using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Vector.Qdrant.Models;

/// <summary>
/// Internal DTO for Qdrant create collection request.
/// </summary>
internal record QdrantCreateCollectionRequest(
    [property: JsonPropertyName("vectors")] QdrantVectorsConfig Vectors);
