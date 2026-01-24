using System.Text.Json.Serialization;

namespace Bipins.AI.Vectors.Weaviate.Models;

/// <summary>
/// Internal DTOs for Weaviate GraphQL API.
/// </summary>
internal record WeaviateObject(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("class")] string Class,
    [property: JsonPropertyName("properties")] Dictionary<string, object> Properties,
    [property: JsonPropertyName("vector")] float[]? Vector = null);

internal record WeaviateBatchRequest(
    [property: JsonPropertyName("objects")] List<WeaviateObject> Objects);

internal record WeaviateGraphQLRequest(
    [property: JsonPropertyName("query")] string Query,
    [property: JsonPropertyName("variables")] Dictionary<string, object>? Variables = null);

internal record WeaviateGraphQLResponse(
    [property: JsonPropertyName("data")] WeaviateGraphQLData? Data,
    [property: JsonPropertyName("errors")] List<WeaviateGraphQLError>? Errors = null);

internal record WeaviateGraphQLData(
    [property: JsonPropertyName("Get")] Dictionary<string, List<WeaviateObject>>? Get = null);

internal record WeaviateGraphQLError(
    [property: JsonPropertyName("message")] string Message);

internal record WeaviateDeleteRequest(
    [property: JsonPropertyName("match")] WeaviateDeleteMatch Match);

internal record WeaviateDeleteMatch(
    [property: JsonPropertyName("class")] string Class,
    [property: JsonPropertyName("where")] Dictionary<string, object>? Where = null);

