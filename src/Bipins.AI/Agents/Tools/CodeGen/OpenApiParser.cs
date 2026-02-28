using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Agents.Tools.CodeGen;

/// <summary>
/// Parses OpenAPI/Swagger specifications from URLs or content.
/// </summary>
public class OpenApiParser : IOpenApiParser
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenApiParser> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiParser"/> class.
    /// </summary>
    public OpenApiParser(
        IHttpClientFactory httpClientFactory,
        ILogger<OpenApiParser> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OpenApiDocument> ParseAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching OpenAPI specification from {Url}", url);

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

#if NETSTANDARD2_1
            var content = await client.GetStringAsync(url);
#else
            var content = await client.GetStringAsync(url, cancellationToken);
#endif

            _logger.LogDebug("Successfully fetched {Length} characters from {Url}", content.Length, url);

            return await ParseFromContentAsync(content, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching OpenAPI specification from {Url}", url);
            throw new InvalidOperationException($"Failed to fetch OpenAPI specification from {url}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout fetching OpenAPI specification from {Url}", url);
            throw new InvalidOperationException($"Timeout fetching OpenAPI specification from {url}", ex);
        }
    }

    /// <inheritdoc />
    public Task<OpenApiDocument> ParseFromContentAsync(string content, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Parsing OpenAPI specification content");

            var reader = new OpenApiStringReader();
            var document = reader.Read(content, out var diagnostic);

            if (diagnostic.Errors.Count > 0)
            {
                var errors = string.Join(", ", diagnostic.Errors.Select(e => e.Message));
                _logger.LogError("OpenAPI parsing errors: {Errors}", errors);
                throw new InvalidOperationException($"OpenAPI parsing failed with errors: {errors}");
            }

            if (diagnostic.Warnings.Count > 0)
            {
                var warnings = string.Join(", ", diagnostic.Warnings.Select(w => w.Message));
                _logger.LogWarning("OpenAPI parsing warnings: {Warnings}", warnings);
            }

            var pathCount = document.Paths?.Count ?? 0;
            var schemaCount = document.Components?.Schemas?.Count ?? 0;
            
            _logger.LogInformation(
                "Successfully parsed OpenAPI {Version} specification with {PathCount} paths and {SchemaCount} schemas",
                document.Info?.Version ?? "unknown",
                pathCount,
                schemaCount);

            return Task.FromResult(document);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error parsing OpenAPI specification");
            throw new InvalidOperationException($"Failed to parse OpenAPI specification: {ex.Message}", ex);
        }
    }
}
