using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Vector.Qdrant.Models;

/// <summary>
/// Internal DTO for Qdrant collection info.
/// </summary>
internal record QdrantCollectionInfo(
    [property: JsonPropertyName("config")] QdrantCollectionConfig? Config);
