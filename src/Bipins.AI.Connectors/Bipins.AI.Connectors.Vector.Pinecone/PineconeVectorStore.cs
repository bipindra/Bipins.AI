using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bipins.AI.Core.Vector;
using Bipins.AI.Connectors.Vector.Pinecone.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Connectors.Vector.Pinecone;

/// <summary>
/// Pinecone implementation of IVectorStore.
/// </summary>
public class PineconeVectorStore : IVectorStore
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PineconeVectorStore> _logger;
    private readonly PineconeOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="PineconeVectorStore"/> class.
    /// </summary>
    public PineconeVectorStore(
        IHttpClientFactory httpClientFactory,
        IOptions<PineconeOptions> options,
        ILogger<PineconeVectorStore> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task UpsertAsync(VectorUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var indexName = request.CollectionName ?? _options.DefaultIndexName;
        var client = CreateHttpClient(indexName);
        var url = $"/vectors/upsert";

        var vectors = request.Records.Select(r =>
        {
            var metadata = new Dictionary<string, object>();
            if (r.Metadata != null)
            {
                foreach (var kvp in r.Metadata)
                {
                    metadata[kvp.Key] = kvp.Value;
                }
            }
            if (!string.IsNullOrEmpty(r.Text))
            {
                metadata["text"] = r.Text;
            }
            if (!string.IsNullOrEmpty(r.SourceUri))
            {
                metadata["sourceUri"] = r.SourceUri;
            }
            if (!string.IsNullOrEmpty(r.DocId))
            {
                metadata["docId"] = r.DocId;
            }
            if (!string.IsNullOrEmpty(r.ChunkId))
            {
                metadata["chunkId"] = r.ChunkId;
            }
            if (!string.IsNullOrEmpty(r.TenantId))
            {
                metadata["tenantId"] = r.TenantId;
            }
            if (!string.IsNullOrEmpty(r.VersionId))
            {
                metadata["versionId"] = r.VersionId;
            }

            return new PineconeVector(
                r.Id,
                r.Vector.ToArray(),
                metadata.Count > 0 ? metadata : null);
        }).ToList();

        var pineconeRequest = new PineconeUpsertRequest(vectors);

        try
        {
            var response = await client.PostAsJsonAsync(url, pineconeRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Upserted {Count} vectors to index {Index}",
                vectors.Count,
                indexName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to upsert vectors to Pinecone");
            throw new PineconeException($"Failed to upsert vectors: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<VectorQueryResponse> QueryAsync(VectorQueryRequest request, CancellationToken cancellationToken = default)
    {
        var indexName = request.CollectionName ?? _options.DefaultIndexName;
        var client = CreateHttpClient(indexName);
        var url = $"/query";

        var filter = request.Filter != null
            ? PineconeFilterTranslator.Translate(request.Filter)
            : null;

        var pineconeRequest = new PineconeQueryRequest(
            request.QueryVector.ToArray(),
            request.TopK,
            IncludeMetadata: true,
            IncludeValues: false,
            Filter: filter);

        try
        {
            var response = await client.PostAsJsonAsync(url, pineconeRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var pineconeResponse = await response.Content.ReadFromJsonAsync<PineconeQueryResponse>(
                cancellationToken: cancellationToken);

            if (pineconeResponse == null)
            {
                throw new PineconeException("Empty response from Pinecone");
            }

            var matches = pineconeResponse.Matches.Select(m =>
            {
                var metadata = m.Metadata ?? new Dictionary<string, object>();
                var record = new VectorRecord(
                    m.Id,
                    Array.Empty<float>().AsMemory(), // Vector not returned by default
                    metadata.TryGetValue("text", out var text) ? text.ToString() ?? string.Empty : string.Empty,
                    metadata,
                    metadata.TryGetValue("sourceUri", out var uri) ? uri.ToString() : null,
                    metadata.TryGetValue("docId", out var docId) ? docId.ToString() : null,
                    metadata.TryGetValue("chunkId", out var chunkId) ? chunkId.ToString() : null,
                    metadata.TryGetValue("tenantId", out var tenantId) ? tenantId.ToString() : null,
                    metadata.TryGetValue("versionId", out var versionId) ? versionId.ToString() : null);

                return new VectorMatch(record, m.Score);
            }).ToList();

            _logger.LogDebug(
                "Query returned {Count} matches from index {Index}",
                matches.Count,
                indexName);

            return new VectorQueryResponse(matches);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to query Pinecone");
            throw new PineconeException($"Failed to query: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(VectorDeleteRequest request, CancellationToken cancellationToken = default)
    {
        var indexName = request.CollectionName ?? _options.DefaultIndexName;
        var client = CreateHttpClient(indexName);
        var url = $"/vectors/delete";

        var pineconeRequest = new PineconeDeleteRequest(
            request.Ids?.ToList(),
            DeleteAll: request.Ids == null || request.Ids.Count == 0,
            Filter: null);

        try
        {
            var response = await client.PostAsJsonAsync(url, pineconeRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Deleted vectors from index {Index}",
                indexName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to delete vectors from Pinecone");
            throw new PineconeException($"Failed to delete vectors: {ex.Message}", ex);
        }
    }

    private HttpClient CreateHttpClient(string indexName)
    {
        var client = _httpClientFactory.CreateClient();
        // Pinecone uses index-specific endpoints: https://{index-name}-{project-id}.svc.{environment}.pinecone.io
        var baseUrl = $"https://{indexName}-{_options.Environment}.svc.{_options.Environment}.pinecone.io";
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("Api-Key", _options.ApiKey);
        client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        return client;
    }
}
