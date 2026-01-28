using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bipins.AI.Vector;
using Bipins.AI.Vectors.Weaviate.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Vectors.Weaviate;

/// <summary>
/// Weaviate implementation of IVectorStore.
/// </summary>
public class WeaviateVectorStore : IVectorStore
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WeaviateVectorStore> _logger;
    private readonly WeaviateOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeaviateVectorStore"/> class.
    /// </summary>
    public WeaviateVectorStore(
        IHttpClientFactory httpClientFactory,
        IOptions<WeaviateOptions> options,
        ILogger<WeaviateVectorStore> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task UpsertAsync(VectorUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var className = request.CollectionName ?? _options.DefaultClassName;
        var client = CreateHttpClient();
        var url = $"/v1/objects";

        var objects = request.Records.Select(r =>
        {
            var properties = new Dictionary<string, object>();
            if (r.Metadata != null)
            {
                foreach (var kvp in r.Metadata)
                {
                    properties[kvp.Key] = kvp.Value;
                }
            }
            if (!string.IsNullOrEmpty(r.Text))
            {
                properties["text"] = r.Text;
            }
            if (!string.IsNullOrEmpty(r.SourceUri))
            {
                properties["sourceUri"] = r.SourceUri;
            }
            if (!string.IsNullOrEmpty(r.DocId))
            {
                properties["docId"] = r.DocId;
            }
            if (!string.IsNullOrEmpty(r.ChunkId))
            {
                properties["chunkId"] = r.ChunkId;
            }
            if (!string.IsNullOrEmpty(r.TenantId))
            {
                properties["tenantId"] = r.TenantId;
            }
            if (!string.IsNullOrEmpty(r.VersionId))
            {
                properties["versionId"] = r.VersionId;
            }

            return new WeaviateObject(
                r.Id,
                className,
                properties,
                r.Vector.ToArray());
        }).ToList();

        try
        {
            // Weaviate batch API
            var batchUrl = $"/v1/batch/objects";
            var batchRequest = new WeaviateBatchRequest(objects);

            var response = await client.PostAsJsonAsync(batchUrl, batchRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Upserted {Count} objects to class {Class}",
                objects.Count,
                className);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to upsert objects to Weaviate");
            throw new WeaviateException($"Failed to upsert objects: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<VectorQueryResponse> QueryAsync(VectorQueryRequest request, CancellationToken cancellationToken = default)
    {
        var className = request.CollectionName ?? _options.DefaultClassName;
        var client = CreateHttpClient();


        // Build GraphQL query
        var vectorArray = request.QueryVector.ToArray();
        var vectorJson = JsonSerializer.Serialize(vectorArray);
        
        var whereClause = "";
        if (request.Filter != null)
        {
            var whereDict = WeaviateFilterTranslator.Translate(request.Filter);
            whereClause = $", where: {JsonSerializer.Serialize(whereDict)}";
        }

        var query = $@"
{{
  Get {{
    {className}(
      nearVector: {{
        vector: {vectorJson}
      }}
      limit: {request.TopK}{whereClause}
    ) {{
      _additional {{
        id
        distance
      }}
      text
      sourceUri
      docId
      chunkId
      tenantId
      versionId
    }}
  }}
}}";

        var graphqlRequest = new WeaviateGraphQLRequest(query);

        try
        {
            var response = await client.PostAsJsonAsync("/v1/graphql", graphqlRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var graphqlResponse = await response.Content.ReadFromJsonAsync<WeaviateGraphQLResponse>(
                cancellationToken: cancellationToken);

            if (graphqlResponse?.Errors != null && graphqlResponse.Errors.Count > 0)
            {
                var errorMessages = string.Join(", ", graphqlResponse.Errors.Select(e => e.Message));
                throw new WeaviateException($"GraphQL errors: {errorMessages}");
            }

            if (graphqlResponse?.Data?.Get == null || !graphqlResponse.Data.Get.ContainsKey(className))
            {
                return new VectorQueryResponse(Array.Empty<VectorMatch>());
            }

            var objects = graphqlResponse.Data.Get[className];
            var matches = objects.Select((obj, index) =>
            {
                var distance = 1.0f; // Weaviate returns distance, convert to similarity score
                if (obj.Properties.TryGetValue("_additional", out var additional) && additional is JsonElement additionalElem)
                {
                    if (additionalElem.TryGetProperty("distance", out var distanceElem))
                    {
                        distance = (float)distanceElem.GetDouble();
                    }
                }

                var score = 1.0f - distance; // Convert distance to similarity

                var record = new VectorRecord(
                    obj.Id,
                    Array.Empty<float>().AsMemory(),
                    obj.Properties.TryGetValue("text", out var text) ? text.ToString() ?? string.Empty : string.Empty,
                    obj.Properties,
                    obj.Properties.TryGetValue("sourceUri", out var uri) ? uri.ToString() : null,
                    obj.Properties.TryGetValue("docId", out var docId) ? docId.ToString() : null,
                    obj.Properties.TryGetValue("chunkId", out var chunkId) ? chunkId.ToString() : null,
                    obj.Properties.TryGetValue("tenantId", out var tenantId) ? tenantId.ToString() : null,
                    obj.Properties.TryGetValue("versionId", out var versionId) ? versionId.ToString() : null);

                return new VectorMatch(record, score);
            }).ToList();

            _logger.LogDebug(
                "Query returned {Count} matches from class {Class}",
                matches.Count,
                className);

            return new VectorQueryResponse(matches);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to query Weaviate");
            throw new WeaviateException($"Failed to query: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(VectorDeleteRequest request, CancellationToken cancellationToken = default)
    {
        var className = request.CollectionName ?? _options.DefaultClassName;
        var client = CreateHttpClient();

        try
        {
            if (request.Ids != null && request.Ids.Count > 0)
            {
                // Delete by IDs
                foreach (var id in request.Ids)
                {
                    var url = $"/v1/objects/{id}";
                    var response = await client.DeleteAsync(url, cancellationToken);
                    response.EnsureSuccessStatusCode();
                }

                _logger.LogInformation(
                    "Deleted {Count} objects from class {Class}",
                    request.Ids.Count,
                    className);
            }
            else
            {
                // Delete all - use batch delete with where clause
                var whereClause = new Dictionary<string, object>
                {
                    ["path"] = new[] { "id" },
                    ["operator"] = "Like",
                    ["valueText"] = "*"
                };

                var deleteRequest = new WeaviateDeleteRequest(
                    new WeaviateDeleteMatch(className, whereClause));

                var url = $"/v1/batch/objects";
                var json = JsonSerializer.Serialize(deleteRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var requestMessage = new HttpRequestMessage(HttpMethod.Delete, url) { Content = content };
                var response = await client.SendAsync(requestMessage, cancellationToken);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation(
                    "Deleted all objects from class {Class}",
                    className);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to delete objects from Weaviate");
            throw new WeaviateException($"Failed to delete objects: {ex.Message}", ex);
        }
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_options.Endpoint);
        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
        client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        return client;
    }
}

