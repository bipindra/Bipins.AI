using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Bipins.AI.Core.Models;
using Bipins.AI.Connectors.Llm.OpenAI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bipins.AI.Connectors.Llm.OpenAI;

/// <summary>
/// OpenAI implementation of streaming chat model.
/// </summary>
public class OpenAiChatModelStreaming : IChatModelStreaming
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenAiChatModelStreaming> _logger;
    private readonly OpenAiOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiChatModelStreaming"/> class.
    /// </summary>
    public OpenAiChatModelStreaming(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiChatModelStreaming> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseChunk> GenerateStreamAsync(ChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
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

        var modelId = request.Metadata?.TryGetValue("modelId", out var modelIdValue) == true
            ? modelIdValue.ToString() ?? _options.DefaultChatModelId
            : _options.DefaultChatModelId;

        var openAiRequest = new OpenAiChatRequest(
            modelId,
            messages,
            request.Temperature,
            request.MaxTokens,
            tools,
            request.ToolChoice != null ? new { type = request.ToolChoice } : null);

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

        // Add stream parameter to request
        var requestJsonWithStream = JsonSerializer.Serialize(new
        {
            model = modelId,
            messages = messages,
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            tools = tools,
            tool_choice = request.ToolChoice != null ? new { type = request.ToolChoice } : null,
            response_format = responseFormat,
            stream = true
        });
        var requestContent = new StringContent(requestJsonWithStream, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, requestContent, cancellationToken);
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

            // OpenAI streaming format: "data: {...}\n\n"
            if (line.StartsWith("data: "))
            {
                var json = line.Substring(6); // Remove "data: " prefix

                if (json == "[DONE]")
                {
                    // Final chunk with usage if available
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

                var chunkResponse = JsonSerializer.Deserialize<OpenAiStreamChunk>(json);
                if (chunkResponse?.Choices != null && chunkResponse.Choices.Count > 0)
                {
                    var choice = chunkResponse.Choices[0];
                    var delta = choice.Delta;

                    if (delta != null && !string.IsNullOrEmpty(delta.Content))
                    {
                        yield return new ChatResponseChunk(
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
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        return client;
    }
}

/// <summary>
/// Internal DTO for OpenAI streaming chunk.
/// </summary>
internal record OpenAiStreamChunk(
    string Id,
    string Model,
    List<OpenAiStreamChoice> Choices,
    OpenAiUsage? Usage);

/// <summary>
/// Internal DTO for OpenAI streaming choice.
/// </summary>
internal record OpenAiStreamChoice(
    OpenAiChatMessage? Delta,
    string? FinishReason);
