using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bipins.AI.Core.Models;
using Bipins.AI.Providers.AzureOpenAI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Providers.AzureOpenAI;

/// <summary>
/// Azure OpenAI implementation of IEmbeddingModel.
/// </summary>
public class AzureOpenAiEmbeddingModel : IEmbeddingModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AzureOpenAiEmbeddingModel> _logger;
    private readonly AzureOpenAiOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAiEmbeddingModel"/> class.
    /// </summary>
    public AzureOpenAiEmbeddingModel(
        IHttpClientFactory httpClientFactory,
        IOptions<AzureOpenAiOptions> options,
        ILogger<AzureOpenAiEmbeddingModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<EmbeddingResponse> EmbedAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
    {
        var client = CreateHttpClient();
        
        var deploymentName = request.ModelId ?? _options.DefaultEmbeddingDeploymentName;
        var url = $"{_options.Endpoint.TrimEnd('/')}/openai/deployments/{deploymentName}/embeddings?api-version={_options.ApiVersion}";

        var azureRequest = new AzureOpenAiEmbeddingRequest(deploymentName, request.Inputs.ToList());

        var attempt = 0;
        while (attempt < _options.MaxRetries)
        {
            try
            {
                var response = await client.PostAsJsonAsync(url, azureRequest, cancellationToken);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = GetRetryAfter(response);
                    if (attempt < _options.MaxRetries - 1 && retryAfter.HasValue)
                    {
                        _logger.LogWarning(
                            "Rate limited. Retrying after {RetryAfter} seconds (attempt {Attempt}/{MaxRetries})",
                            retryAfter.Value,
                            attempt + 1,
                            _options.MaxRetries);
                        await Task.Delay(TimeSpan.FromSeconds(retryAfter.Value), cancellationToken);
                        attempt++;
                        continue;
                    }
                }

                response.EnsureSuccessStatusCode();

                var azureResponse = await response.Content.ReadFromJsonAsync<AzureOpenAiEmbeddingResponse>(
                    cancellationToken: cancellationToken);

                if (azureResponse == null || azureResponse.Data.Count == 0)
                {
                    throw new AzureOpenAiException("Empty response from Azure OpenAI");
                }

                var vectors = azureResponse.Data
                    .OrderBy(d => d.Index)
                    .Select(d => (ReadOnlyMemory<float>)d.Embedding.ToArray().AsMemory())
                    .ToList();

                var usage = azureResponse.Usage != null
                    ? new Usage(
                        azureResponse.Usage.PromptTokens,
                        azureResponse.Usage.CompletionTokens,
                        azureResponse.Usage.TotalTokens)
                    : null;

                return new EmbeddingResponse(vectors, usage, azureResponse.Model);
            }
            catch (HttpRequestException ex) when (IsRetryable(ex) && attempt < _options.MaxRetries - 1)
            {
                attempt++;
                var delay = CalculateBackoff(attempt);
                _logger.LogWarning(
                    ex,
                    "Request failed (attempt {Attempt}/{MaxRetries}). Retrying in {Delay}ms",
                    attempt,
                    _options.MaxRetries,
                    delay);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new AzureOpenAiException("Failed to generate embeddings after retries");
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_options.Endpoint);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("api-key", _options.ApiKey);
        client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        return client;
    }

    private static bool IsRetryable(HttpRequestException ex)
    {
        return ex.Data.Contains("StatusCode") &&
               ex.Data["StatusCode"] is int statusCode &&
               statusCode >= 500;
    }

    private static int? GetRetryAfter(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter?.Delta.HasValue == true)
        {
            return (int)response.Headers.RetryAfter.Delta.Value.TotalSeconds;
        }

        if (response.Headers.RetryAfter?.Date.HasValue == true)
        {
            var seconds = (int)(response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow).TotalSeconds;
            return seconds > 0 ? seconds : null;
        }

        return null;
    }

    private static int CalculateBackoff(int attempt)
    {
        return (int)(Math.Pow(2, attempt) * 1000);
    }
}

