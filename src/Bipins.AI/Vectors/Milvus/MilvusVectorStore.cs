using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Bipins.AI.Vector;
using Bipins.AI.Vectors.Milvus.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Vectors.Milvus;

/// <summary>
/// Milvus implementation of IVectorStore using HTTP API.
/// </summary>
public class MilvusVectorStore : IVectorStore
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MilvusVectorStore> _logger;
    private readonly MilvusOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MilvusVectorStore"/> class.
    /// </summary>
    public MilvusVectorStore(
        IHttpClientFactory httpClientFactory,
        IOptions<MilvusOptions> options,
        ILogger<MilvusVectorStore> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task UpsertAsync(VectorUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var collectionName = request.CollectionName ?? _options.DefaultCollectionName;
        var client = CreateHttpClient();
        
        try
        {
            // Ensure collection exists
            await EnsureCollectionExistsAsync(collectionName, cancellationToken);

            // Prepare data for Milvus insert
            var insertData = new MilvusInsertRequest(
                collectionName,
                request.Records.Select(r => new MilvusInsertRecord(
                    r.Id,
                    r.Vector.ToArray(),
                    r.Text ?? string.Empty,
                    r.SourceUri ?? string.Empty,
                    r.DocId ?? string.Empty,
                    r.ChunkId ?? string.Empty,
                    r.TenantId ?? string.Empty,
                    r.VersionId ?? string.Empty,
                    r.Metadata ?? new Dictionary<string, object>())).ToList());

            var url = $"/v1/vector/insert";
            var response = await client.PostAsJsonAsync(url, insertData, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Upserted {Count} records to collection {Collection}",
                request.Records.Count,
                collectionName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to upsert records to Milvus");
            throw new MilvusException($"Failed to upsert records: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<VectorQueryResponse> QueryAsync(VectorQueryRequest request, CancellationToken cancellationToken = default)
    {
        var collectionName = request.CollectionName ?? _options.DefaultCollectionName;
        var client = CreateHttpClient();

        try
        {
            var filterExpression = request.Filter != null
                ? MilvusFilterTranslator.Translate(request.Filter)
                : null;

            var searchRequest = new MilvusSearchRequest(
                collectionName,
                request.QueryVector.ToArray(),
                request.TopK,
                filterExpression);

            var url = $"/v1/vector/search";
            var response = await client.PostAsJsonAsync(url, searchRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var searchResponse = await response.Content.ReadFromJsonAsync<MilvusSearchResponse>(
                cancellationToken: cancellationToken);

            if (searchResponse == null)
            {
                throw new MilvusException("Empty response from Milvus");
            }

            var matches = searchResponse.Results.Select(r =>
            {
                var metadata = r.Metadata ?? new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(r.Text))
                {
                    metadata["text"] = r.Text;
                }

                var record = new VectorRecord(
                    r.Id,
                    Array.Empty<float>().AsMemory(),
                    r.Text ?? string.Empty,
                    metadata,
                    r.SourceUri,
                    r.DocId,
                    r.ChunkId,
                    r.TenantId,
                    r.VersionId);

                return new VectorMatch(record, r.Score);
            }).ToList();

            _logger.LogDebug(
                "Query returned {Count} matches from collection {Collection}",
                matches.Count,
                collectionName);

            return new VectorQueryResponse(matches);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to query Milvus");
            throw new MilvusException($"Failed to query: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(VectorDeleteRequest request, CancellationToken cancellationToken = default)
    {
        var collectionName = request.CollectionName ?? _options.DefaultCollectionName;
        var client = CreateHttpClient();

        try
        {
            if (request.Ids != null && request.Ids.Count > 0)
            {
                var deleteRequest = new MilvusDeleteRequest(
                    collectionName,
                    request.Ids.ToList());

                var url = $"/v1/vector/delete";
                var response = await client.PostAsJsonAsync(url, deleteRequest, cancellationToken);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation(
                    "Deleted {Count} records from collection {Collection}",
                    request.Ids.Count,
                    collectionName);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to delete records from Milvus");
            throw new MilvusException($"Failed to delete records: {ex.Message}", ex);
        }
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        var baseUrl = $"http://{_options.Endpoint}";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        return client;
    }

    private async Task EnsureCollectionExistsAsync(string collectionName, CancellationToken cancellationToken)
    {
        var client = CreateHttpClient();
        
        try
        {
            // Check if collection exists
            var checkUrl = $"/v1/collection/{collectionName}";
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
            var createRequest = new MilvusCreateCollectionRequest(
                collectionName,
                _options.VectorSize);

            var createUrl = $"/v1/collection/create";
            var createResponse = await client.PostAsJsonAsync(createUrl, createRequest, cancellationToken);
            createResponse.EnsureSuccessStatusCode();

            _logger.LogInformation("Created collection {Collection}", collectionName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to ensure collection {Collection} exists", collectionName);
            throw new MilvusException($"Failed to create collection: {ex.Message}", ex);
        }
    }
}

