using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bipins.AI.Core.Models;
using Bipins.AI.Providers.Anthropic.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Providers.Anthropic;

/// <summary>
/// Anthropic Claude implementation of IChatModel.
/// </summary>
public class AnthropicChatModel : IChatModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AnthropicChatModel> _logger;
    private readonly AnthropicOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicChatModel"/> class.
    /// </summary>
    public AnthropicChatModel(
        IHttpClientFactory httpClientFactory,
        IOptions<AnthropicOptions> options,
        ILogger<AnthropicChatModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GenerateAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var client = CreateHttpClient();
        var url = $"{_options.BaseUrl}/messages";

        var modelId = request.Metadata?.TryGetValue("modelId", out var modelIdValue) == true
            ? modelIdValue.ToString() ?? _options.DefaultChatModelId
            : _options.DefaultChatModelId;

        // Separate system messages from regular messages
        var systemMessages = request.Messages
            .Where(m => m.Role == MessageRole.System)
            .Select(m => m.Content)
            .ToList();
        var systemText = systemMessages.Count > 0 ? string.Join("\n", systemMessages) : null;

        var regularMessages = request.Messages
            .Where(m => m.Role != MessageRole.System)
            .ToList();

        var anthropicMessages = regularMessages.Select(m => new AnthropicMessage(
            m.Role switch
            {
                MessageRole.User => "user",
                MessageRole.Assistant => "assistant",
                MessageRole.Tool => "user", // Anthropic doesn't have a tool role, map to user
                _ => "user"
            },
            m.Content)).ToList();

        // Map tools to Anthropic format
        List<AnthropicTool>? anthropicTools = null;
        if (request.Tools != null && request.Tools.Count > 0)
        {
            anthropicTools = request.Tools.Select(t => new AnthropicTool(
                t.Name,
                t.Description,
                t.Parameters)).ToList();
        }

        var maxTokens = request.MaxTokens ?? 4096;
        var anthropicRequest = new AnthropicChatRequest(
            modelId,
            maxTokens,
            anthropicMessages,
            systemText,
            request.Temperature,
            anthropicTools);

        var attempt = 0;
        while (attempt < _options.MaxRetries)
        {
            try
            {
                var response = await client.PostAsJsonAsync(url, anthropicRequest, cancellationToken);

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

                var anthropicResponse = await response.Content.ReadFromJsonAsync<AnthropicChatResponse>(
                    cancellationToken: cancellationToken);

                if (anthropicResponse == null || anthropicResponse.Content.Count == 0)
                {
                    throw new AnthropicException("Empty response from Anthropic");
                }

                // Extract text content from content blocks
                var textContent = string.Join("", anthropicResponse.Content
                    .Where(c => c.Type == "text")
                    .Select(c => c.Text ?? string.Empty));

                // Extract tool calls from tool_use content blocks
                var toolCalls = new List<ToolCall>();
                foreach (var contentBlock in anthropicResponse.Content)
                {
                    if (contentBlock.Type == "tool_use" && contentBlock.Id != null && contentBlock.Name != null)
                    {
                        System.Text.Json.JsonElement argumentsJson;
                        try
                        {
                            if (contentBlock.Input != null)
                            {
                                // Convert input object to JsonElement
                                var inputJson = System.Text.Json.JsonSerializer.Serialize(contentBlock.Input);
                                argumentsJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(inputJson);
                            }
                            else
                            {
                                argumentsJson = System.Text.Json.JsonSerializer.SerializeToElement(new { });
                            }
                        }
                        catch
                        {
                            argumentsJson = System.Text.Json.JsonSerializer.SerializeToElement(new { });
                        }

                        toolCalls.Add(new ToolCall(
                            contentBlock.Id,
                            contentBlock.Name,
                            argumentsJson));
                    }
                }

                var usage = anthropicResponse.Usage != null
                    ? new Usage(
                        anthropicResponse.Usage.InputTokens,
                        0, // Anthropic doesn't separate completion tokens
                        anthropicResponse.Usage.InputTokens + anthropicResponse.Usage.OutputTokens)
                    : null;

                return new ChatResponse(
                    textContent,
                    toolCalls.Count > 0 ? toolCalls : null,
                    usage,
                    anthropicResponse.Model,
                    anthropicResponse.StopReason);
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

        throw new AnthropicException("Failed to generate chat completion after retries");
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_options.BaseUrl);
        client.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", _options.ApiVersion);
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
        return (int)(Math.Pow(2, attempt) * 1000); // Exponential backoff
    }
}

