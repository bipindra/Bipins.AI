using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bipins.AI.Core.Models;
using Bipins.AI.Providers.OpenAI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Providers.OpenAI;

/// <summary>
/// OpenAI implementation of IEmbeddingModel.
/// </summary>
public class OpenAiEmbeddingModel : IEmbeddingModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenAiEmbeddingModel> _logger;
    private readonly OpenAiOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiEmbeddingModel"/> class.
    /// </summary>
    public OpenAiEmbeddingModel(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiEmbeddingModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<EmbeddingResponse> EmbedAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
    {
        var client = CreateHttpClient();
        var url = $"{_options.BaseUrl}/embeddings";

        var modelId = request.ModelId ?? _options.DefaultEmbeddingModelId;
        var openAiRequest = new OpenAiEmbeddingRequest(modelId, request.Inputs.ToList());

        var attempt = 0;
        while (attempt < _options.MaxRetries)
        {
            try
            {
                var response = await client.PostAsJsonAsync(url, openAiRequest, cancellationToken);

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

                var openAiResponse = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingResponse>(
                    cancellationToken: cancellationToken);

                if (openAiResponse == null || openAiResponse.Data.Count == 0)
                {
                    throw new OpenAiException("Empty response from OpenAI");
                }

                var vectors = openAiResponse.Data
                    .OrderBy(d => d.Index)
                    .Select(d => (ReadOnlyMemory<float>)d.Embedding.AsMemory())
                    .ToList();

                var usage = openAiResponse.Usage != null
                    ? new Usage(
                        openAiResponse.Usage.PromptTokens,
                        openAiResponse.Usage.CompletionTokens,
                        openAiResponse.Usage.TotalTokens)
                    : null;

                return new EmbeddingResponse(vectors, usage, openAiResponse.Model);
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

        throw new OpenAiException("Failed to generate embeddings after retries");
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_options.BaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
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

