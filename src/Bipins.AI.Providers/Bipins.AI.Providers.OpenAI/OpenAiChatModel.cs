using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Bipins.AI.Core.Models;
using Bipins.AI.Providers.OpenAI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Providers.OpenAI;

/// <summary>
/// OpenAI implementation of IChatModel.
/// </summary>
public class OpenAiChatModel : IChatModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenAiChatModel> _logger;
    private readonly OpenAiOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiChatModel"/> class.
    /// </summary>
    public OpenAiChatModel(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiChatModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GenerateAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var client = CreateHttpClient();
        var url = $"{_options.BaseUrl}/chat/completions";

        var messages = request.Messages.Select(m => new OpenAiChatMessage(
            m.Role.ToString().ToLowerInvariant(),
            m.Content,
            m.ToolCallId)).ToList();

        var tools = request.Tools?.Select(t => new OpenAiTool(
            "function",
            new OpenAiFunction(t.Name, t.Description, t.Parameters))).ToList();

        // Handle structured output
        object? responseFormat = null;
        if (request.StructuredOutput != null)
        {
            responseFormat = new
            {
                type = request.StructuredOutput.ResponseFormat,
                json_schema = request.StructuredOutput.Schema
            };
        }

        var openAiRequest = new OpenAiChatRequest(
            request.Metadata?.TryGetValue("modelId", out var modelId) == true
                ? modelId.ToString() ?? _options.DefaultChatModelId
                : _options.DefaultChatModelId,
            messages,
            request.Temperature,
            request.MaxTokens,
            tools,
            request.ToolChoice != null ? new { type = request.ToolChoice } : null,
            responseFormat);

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

                var openAiResponse = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(
                    cancellationToken: cancellationToken);

                if (openAiResponse == null || openAiResponse.Choices.Count == 0)
                {
                    throw new OpenAiException("Empty response from OpenAI");
                }

                var choice = openAiResponse.Choices[0];
                var message = choice.Message ?? throw new OpenAiException("No message in response");

                // Parse tool calls from message
                var toolCalls = new List<ToolCall>();
                if (message.ToolCalls != null && message.ToolCalls.Count > 0)
                {
                    foreach (var toolCall in message.ToolCalls)
                    {
                        JsonElement argumentsJson;
                        try
                        {
                            argumentsJson = JsonSerializer.Deserialize<JsonElement>(toolCall.Function.Arguments);
                        }
                        catch
                        {
                            // If parsing fails, wrap in a JSON element
                            argumentsJson = JsonSerializer.SerializeToElement(toolCall.Function.Arguments);
                        }

                        toolCalls.Add(new ToolCall(
                            toolCall.Id,
                            toolCall.Function.Name,
                            argumentsJson));
                    }
                }

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

        throw new OpenAiException("Failed to generate chat completion after retries");
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
        // Retry on 5xx or network errors
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
        return (int)(Math.Pow(2, attempt) * 1000); // Exponential backoff
    }
}

