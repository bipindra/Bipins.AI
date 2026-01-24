using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bipins.AI.Core.Models;
using Bipins.AI.Connectors.Llm.Anthropic.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Connectors.Llm.Anthropic;

/// <summary>
/// Anthropic Claude implementation of streaming chat model.
/// </summary>
public class AnthropicChatModelStreaming : IChatModelStreaming
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AnthropicChatModelStreaming> _logger;
    private readonly AnthropicOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicChatModelStreaming"/> class.
    /// </summary>
    public AnthropicChatModelStreaming(
        IHttpClientFactory httpClientFactory,
        IOptions<AnthropicOptions> options,
        ILogger<AnthropicChatModelStreaming> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseChunk> GenerateStreamAsync(
        ChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
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
                MessageRole.Tool => "user",
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

        // Add stream parameter
        var requestJson = JsonSerializer.Serialize(anthropicRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        // Anthropic uses Server-Sent Events (SSE) format
        var requestContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
        requestContent.Headers.Add("anthropic-version", _options.ApiVersion);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = requestContent
        };
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", _options.ApiVersion);

        var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await foreach (var chunk in ParseStreamingResponse(response, modelId, cancellationToken))
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<ChatResponseChunk> ParseStreamingResponse(
        HttpResponseMessage response,
        string modelId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Usage? cumulativeUsage = null;
        string? finishReason = null;

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Anthropic SSE format: "event: {...}\ndata: {...}\n\n"
            if (line.StartsWith("data: "))
            {
                var json = line.Substring(6); // Remove "data: " prefix

                ChatResponseChunk? chunkToYield = null;
                try
                {
                    var chunkResponse = JsonSerializer.Deserialize<AnthropicStreamChunk>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (chunkResponse?.Type == "content_block_delta" && chunkResponse.Delta != null)
                    {
                        if (chunkResponse.Delta.Type == "text_delta" && !string.IsNullOrEmpty(chunkResponse.Delta.Text))
                        {
                            chunkToYield = new ChatResponseChunk(
                                chunkResponse.Delta.Text,
                                IsComplete: false,
                                ModelId: modelId);
                        }
                    }
                    else if (chunkResponse?.Type == "message_stop")
                    {
                        finishReason = "stop";
                    }
                    else if (chunkResponse?.Type == "message_delta" && chunkResponse.Usage != null)
                    {
                        cumulativeUsage = new Usage(
                            chunkResponse.Usage.InputTokens,
                            0,
                            chunkResponse.Usage.InputTokens + chunkResponse.Usage.OutputTokens);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse streaming chunk: {Json}", json);
                }

                if (chunkToYield != null)
                {
                    yield return chunkToYield;
                }
            }
        }

        // Final chunk
        if (cumulativeUsage != null || finishReason != null)
        {
            yield return new ChatResponseChunk(
                string.Empty,
                IsComplete: true,
                ModelId: modelId,
                Usage: cumulativeUsage,
                FinishReason: finishReason);
        }
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
}

/// <summary>
/// Internal DTO for Anthropic streaming chunk.
/// </summary>
internal record AnthropicStreamChunk(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("delta")] AnthropicStreamDelta? Delta = null,
    [property: JsonPropertyName("usage")] AnthropicUsage? Usage = null);

/// <summary>
/// Internal DTO for Anthropic stream delta.
/// </summary>
internal record AnthropicStreamDelta(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string? Text = null);
