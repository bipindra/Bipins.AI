using System.Text.Json.Serialization;

namespace Bipins.AI.Vectors.Milvus.Models;

/// <summary>
/// Internal DTOs for Milvus HTTP API.
/// </summary>
internal record MilvusInsertRecord(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("vector")] float[] Vector,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("sourceUri")] string SourceUri,
    [property: JsonPropertyName("docId")] string DocId,
    [property: JsonPropertyName("chunkId")] string ChunkId,
    [property: JsonPropertyName("tenantId")] string TenantId,
    [property: JsonPropertyName("versionId")] string VersionId,
    [property: JsonPropertyName("metadata")] Dictionary<string, object> Metadata);

internal record MilvusInsertRequest(
    [property: JsonPropertyName("collection_name")] string CollectionName,
    [property: JsonPropertyName("data")] List<MilvusInsertRecord> Data);

internal record MilvusSearchRequest(
    [property: JsonPropertyName("collection_name")] string CollectionName,
    [property: JsonPropertyName("vector")] float[] Vector,
    [property: JsonPropertyName("top_k")] int TopK,
    [property: JsonPropertyName("expr")] string? Expression = null);

internal record MilvusSearchResponse(
    [property: JsonPropertyName("results")] List<MilvusSearchResult> Results);

internal record MilvusSearchResult(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("score")] float Score,
    [property: JsonPropertyName("text")] string? Text = null,
    [property: JsonPropertyName("sourceUri")] string? SourceUri = null,
    [property: JsonPropertyName("docId")] string? DocId = null,
    [property: JsonPropertyName("chunkId")] string? ChunkId = null,
    [property: JsonPropertyName("tenantId")] string? TenantId = null,
    [property: JsonPropertyName("versionId")] string? VersionId = null,
    [property: JsonPropertyName("metadata")] Dictionary<string, object>? Metadata = null);

internal record MilvusDeleteRequest(
    [property: JsonPropertyName("collection_name")] string CollectionName,
    [property: JsonPropertyName("ids")] List<string> Ids);

internal record MilvusCreateCollectionRequest(
    [property: JsonPropertyName("collection_name")] string CollectionName,
    [property: JsonPropertyName("dimension")] int Dimension);

