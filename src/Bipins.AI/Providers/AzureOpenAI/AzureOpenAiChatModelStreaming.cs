using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Bipins.AI.Core.Models;
using Bipins.AI.Providers.AzureOpenAI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Providers.AzureOpenAI;

/// <summary>
/// Azure OpenAI implementation of streaming chat model.
/// </summary>
public class AzureOpenAiChatModelStreaming : IChatModelStreaming
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AzureOpenAiChatModelStreaming> _logger;
    private readonly AzureOpenAiOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAiChatModelStreaming"/> class.
    /// </summary>
    public AzureOpenAiChatModelStreaming(
        IHttpClientFactory httpClientFactory,
        IOptions<AzureOpenAiOptions> options,
        ILogger<AzureOpenAiChatModelStreaming> logger)
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

        // Azure OpenAI uses same format as OpenAI for streaming
        var requestJson = JsonSerializer.Serialize(new
        {
            messages = messages,
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            tools = tools,
            tool_choice = request.ToolChoice != null ? new { type = request.ToolChoice } : null,
            stream = true
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var requestContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, requestContent, cancellationToken);
        response.EnsureSuccessStatusCode();

        await foreach (var chunk in ParseStreamingResponse(response, deploymentName, cancellationToken))
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

#if NETSTANDARD2_1
        using var stream = await response.Content.ReadAsStreamAsync();
#else
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
#endif
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Azure OpenAI uses same SSE format as OpenAI: "data: {...}\n\n"
            if (line.StartsWith("data: "))
            {
                var json = line.Substring(6); // Remove "data: " prefix

                if (json == "[DONE]")
                {
                    if (cumulativeUsage != null)
                    {
                        yield return new ChatResponseChunk(
                            string.Empty,
                            IsComplete: true,
                            ModelId: modelId,
                            Usage: cumulativeUsage,
                            FinishReason: finishReason);
                    }
                    yield break;
                }

                ChatResponseChunk? chunkToYield = null;
                try
                {
                    var chunkResponse = JsonSerializer.Deserialize<AzureOpenAiStreamChunk>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (chunkResponse?.Choices != null && chunkResponse.Choices.Count > 0)
                    {
                        var choice = chunkResponse.Choices[0];
                        var delta = choice.Delta;

                        if (delta != null && !string.IsNullOrEmpty(delta.Content))
                        {
                            chunkToYield = new ChatResponseChunk(
                                delta.Content,
                                IsComplete: false,
                                ModelId: chunkResponse.Model ?? modelId);
                        }

                        if (!string.IsNullOrEmpty(choice.FinishReason))
                        {
                            finishReason = choice.FinishReason;
                        }
                        
                        if (chunkResponse.Usage != null)
                        {
                            cumulativeUsage = new Usage(
                                chunkResponse.Usage.PromptTokens,
                                chunkResponse.Usage.CompletionTokens,
                                chunkResponse.Usage.TotalTokens);
                        }
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
        client.BaseAddress = new Uri(_options.Endpoint);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("api-key", _options.ApiKey);
        client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        return client;
    }
}

/// <summary>
/// Internal DTO for Azure OpenAI streaming chunk.
/// </summary>
internal record AzureOpenAiStreamChunk(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] string Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("model")] string Model,
    [property: System.Text.Json.Serialization.JsonPropertyName("choices")] List<AzureOpenAiStreamChoice> Choices,
    [property: System.Text.Json.Serialization.JsonPropertyName("usage")] AzureOpenAiUsage? Usage);

/// <summary>
/// Internal DTO for Azure OpenAI streaming choice.
/// </summary>
internal record AzureOpenAiStreamChoice(
    [property: System.Text.Json.Serialization.JsonPropertyName("delta")] AzureOpenAiChatMessage? Delta,
    [property: System.Text.Json.Serialization.JsonPropertyName("finish_reason")] string? FinishReason);

