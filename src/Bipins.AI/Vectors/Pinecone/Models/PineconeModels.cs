using System.Text.Json.Serialization;

namespace Bipins.AI.Vectors.Pinecone.Models;

/// <summary>
/// Internal DTOs for Pinecone API.
/// </summary>
internal record PineconeVector(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("values")] float[] Values,
    [property: JsonPropertyName("metadata")] Dictionary<string, object>? Metadata = null);

internal record PineconeUpsertRequest(
    [property: JsonPropertyName("vectors")] List<PineconeVector> Vectors,
    [property: JsonPropertyName("namespace")] string? Namespace = null);

internal record PineconeQueryRequest(
    [property: JsonPropertyName("vector")] float[] Vector,
    [property: JsonPropertyName("topK")] int TopK,
    [property: JsonPropertyName("includeMetadata")] bool IncludeMetadata = true,
    [property: JsonPropertyName("includeValues")] bool IncludeValues = false,
    [property: JsonPropertyName("filter")] Dictionary<string, object>? Filter = null,
    [property: JsonPropertyName("namespace")] string? Namespace = null);

internal record PineconeQueryResponse(
    [property: JsonPropertyName("matches")] List<PineconeMatch> Matches,
    [property: JsonPropertyName("namespace")] string? Namespace = null);

internal record PineconeMatch(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("score")] float Score,
    [property: JsonPropertyName("metadata")] Dictionary<string, object>? Metadata = null,
    [property: JsonPropertyName("values")] float[]? Values = null);

internal record PineconeDeleteRequest(
    [property: JsonPropertyName("ids")] List<string>? Ids = null,
    [property: JsonPropertyName("deleteAll")] bool DeleteAll = false,
    [property: JsonPropertyName("filter")] Dictionary<string, object>? Filter = null,
    [property: JsonPropertyName("namespace")] string? Namespace = null);

