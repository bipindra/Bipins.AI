using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Bipins.AI.Core.Vector;
using Bipins.AI.Vectors.Qdrant.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Vectors.Qdrant;

/// <summary>
/// Qdrant implementation of IVectorStore.
/// </summary>
public class QdrantVectorStore : IVectorStore
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<QdrantVectorStore> _logger;
    private readonly QdrantOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="QdrantVectorStore"/> class.
    /// </summary>
    public QdrantVectorStore(
        IHttpClientFactory httpClientFactory,
        IOptions<QdrantOptions> options,
        ILogger<QdrantVectorStore> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;

        if (_options.CreateCollectionIfMissing)
        {
            _ = EnsureCollectionExistsAsync(CancellationToken.None);
        }
    }

    /// <inheritdoc />
    public async Task UpsertAsync(VectorUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var collectionName = request.CollectionName ?? _options.DefaultCollectionName;
        await EnsureCollectionExistsAsync(cancellationToken);

        var client = CreateHttpClient();
        var url = $"{_options.Endpoint}/collections/{collectionName}/points";

        var points = request.Records.Select(r => new QdrantPoint(
            r.Id,
            r.Vector.ToArray(),
            r.Metadata)).ToList();

        var qdrantRequest = new QdrantUpsertRequest(points);

        try
        {
            var response = await client.PutAsJsonAsync(url, qdrantRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Upserted {Count} points to collection {Collection}",
                points.Count,
                collectionName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to upsert points to Qdrant");
            throw new QdrantException($"Failed to upsert points: {ex.Message}", ex)
            {
                StatusCode = (int?)ex.Data["StatusCode"]
            };
        }
    }

    /// <inheritdoc />
    public async Task<VectorQueryResponse> QueryAsync(VectorQueryRequest request, CancellationToken cancellationToken = default)
    {
        var collectionName = request.CollectionName ?? _options.DefaultCollectionName;
        var client = CreateHttpClient();
        var url = $"{_options.Endpoint}/collections/{collectionName}/points/search";

        JsonElement? filter = null;
        if (request.Filter != null)
        {
            filter = QdrantFilterTranslator.Translate(request.Filter);
        }

        var qdrantRequest = new QdrantSearchRequest(
            request.QueryVector.ToArray(),
            request.TopK,
            filter);

        try
        {
            var response = await client.PostAsJsonAsync(url, qdrantRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var searchResponse = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);

            if (searchResponse == null)
            {
                throw new QdrantException("Empty response from Qdrant");
            }

            var matches = searchResponse.Result.Select(sp =>
            {
                var metadata = sp.Payload ?? new Dictionary<string, object>();
                var record = new VectorRecord(
                    sp.Id,
                    Array.Empty<float>().AsMemory(), // Vector not returned by default
                    metadata.TryGetValue("text", out var text) ? text.ToString() ?? string.Empty : string.Empty,
                    metadata,
                    metadata.TryGetValue("sourceUri", out var uri) ? uri.ToString() : null,
                    metadata.TryGetValue("docId", out var docId) ? docId.ToString() : null,
                    metadata.TryGetValue("chunkId", out var chunkId) ? chunkId.ToString() : null,
                    metadata.TryGetValue("tenantId", out var tenantId) ? tenantId.ToString() : null,
                    metadata.TryGetValue("versionId", out var versionId) ? versionId.ToString() : null);

                return new VectorMatch(record, sp.Score);
            }).ToList();

            _logger.LogDebug(
                "Query returned {Count} matches from collection {Collection}",
                matches.Count,
                collectionName);

            return new VectorQueryResponse(matches);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to query Qdrant");
            throw new QdrantException($"Failed to query: {ex.Message}", ex)
            {
                StatusCode = (int?)ex.Data["StatusCode"]
            };
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(VectorDeleteRequest request, CancellationToken cancellationToken = default)
    {
        var collectionName = request.CollectionName ?? _options.DefaultCollectionName;
        var client = CreateHttpClient();
        var url = $"{_options.Endpoint}/collections/{collectionName}/points/delete";

        var qdrantRequest = new QdrantDeleteRequest(request.Ids.ToList());

        try
        {
            var response = await client.PostAsJsonAsync(url, qdrantRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Deleted {Count} points from collection {Collection}",
                request.Ids.Count,
                collectionName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to delete points from Qdrant");
            throw new QdrantException($"Failed to delete: {ex.Message}", ex)
            {
                StatusCode = (int?)ex.Data["StatusCode"]
            };
        }
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_options.Endpoint);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            client.DefaultRequestHeaders.Add("api-key", _options.ApiKey);
        }

        return client;
    }

    private async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken)
    {
        var collectionName = _options.DefaultCollectionName;
        var client = CreateHttpClient();

        // Check if collection exists
        var checkUrl = $"{_options.Endpoint}/collections/{collectionName}";
        var checkResponse = await client.GetAsync(checkUrl, cancellationToken);

        if (checkResponse.StatusCode == HttpStatusCode.OK)
        {
            _logger.LogDebug("Collection {Collection} already exists", collectionName);
            return;
        }

        if (checkResponse.StatusCode != HttpStatusCode.NotFound)
        {
            checkResponse.EnsureSuccessStatusCode();
        }

        // Create collection
        var createUrl = $"{_options.Endpoint}/collections/{collectionName}";
        var createRequest = new QdrantCreateCollectionRequest(
            new QdrantVectorsConfig(_options.VectorSize, _options.Distance));

        try
        {
            var createResponse = await client.PutAsJsonAsync(createUrl, createRequest, cancellationToken);
            createResponse.EnsureSuccessStatusCode();
            _logger.LogInformation("Created collection {Collection}", collectionName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to create collection {Collection}", collectionName);
            throw new QdrantException($"Failed to create collection: {ex.Message}", ex);
        }
    }
}

