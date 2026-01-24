using System.Text.Json.Serialization;

namespace Bipins.AI.Connectors.Vector.Qdrant.Models;

/// <summary>
/// Internal DTO for Qdrant collection config.
/// </summary>
internal record QdrantCollectionConfig(
    [property: JsonPropertyName("params")] QdrantCollectionParams? Params);
