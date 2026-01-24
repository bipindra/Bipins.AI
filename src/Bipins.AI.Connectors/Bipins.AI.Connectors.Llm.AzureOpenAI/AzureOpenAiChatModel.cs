using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bipins.AI.Core.Models;
using Bipins.AI.Connectors.Llm.AzureOpenAI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Connectors.Llm.AzureOpenAI;

/// <summary>
/// Azure OpenAI implementation of IChatModel.
/// </summary>
public class AzureOpenAiChatModel : IChatModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AzureOpenAiChatModel> _logger;
    private readonly AzureOpenAiOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAiChatModel"/> class.
    /// </summary>
    public AzureOpenAiChatModel(
        IHttpClientFactory httpClientFactory,
        IOptions<AzureOpenAiOptions> options,
        ILogger<AzureOpenAiChatModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GenerateAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var client = CreateHttpClient();
        
        // Determine deployment name from metadata or use default
        var deploymentName = request.Metadata?.TryGetValue("deploymentName", out var deploymentValue) == true
            ? deploymentValue.ToString() ?? _options.DefaultChatDeploymentName
            : _options.DefaultChatDeploymentName;

        var url = $"{_options.Endpoint.TrimEnd('/')}/openai/deployments/{deploymentName}/chat/completions?api-version={_options.ApiVersion}";

        var messages = request.Messages.Select(m => new AzureOpenAiChatMessage(
            m.Role.ToString().ToLowerInvariant(),
            m.Content,
            m.ToolCallId)).ToList();

        var tools = request.Tools?.Select(t => new AzureOpenAiTool(
            "function",
            new AzureOpenAiFunction(t.Name, t.Description, t.Parameters))).ToList();

        var openAiRequest = new AzureOpenAiChatRequest(
            deploymentName, // Azure uses deployment name instead of model ID
            messages,
            request.Temperature,
            request.MaxTokens,
            tools,
            request.ToolChoice != null ? new { type = request.ToolChoice } : null);

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

                var openAiResponse = await response.Content.ReadFromJsonAsync<AzureOpenAiChatResponse>(
                    cancellationToken: cancellationToken);

                if (openAiResponse == null || openAiResponse.Choices.Count == 0)
                {
                    throw new AzureOpenAiException("Empty response from Azure OpenAI");
                }

                var choice = openAiResponse.Choices[0];
                var message = choice.Message ?? throw new AzureOpenAiException("No message in response");

                var toolCalls = new List<ToolCall>();
                // Tool calls would be parsed from message if present

                var usage = openAiResponse.Usage != null
                    ? new Usage(
                        openAiResponse.Usage.PromptTokens,
                        openAiResponse.Usage.CompletionTokens,
                        openAiResponse.Usage.TotalTokens)
                    : null;

                return new ChatResponse(
                    message.Content ?? string.Empty,
                    toolCalls.Count > 0 ? toolCalls : null,
                    usage,
                    openAiResponse.Model,
                    choice.FinishReason);
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

        throw new AzureOpenAiException("Failed to generate chat completion after retries");
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
