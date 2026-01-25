using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime;
using Amazon.Runtime.EventStreams;
using Bipins.AI.Core.Models;
using Bipins.AI.Providers.Bedrock.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Bipins.AI.Providers.Bedrock;

/// <summary>
/// AWS Bedrock implementation of streaming chat model.
/// </summary>
public class BedrockChatModelStreaming : IChatModelStreaming
{
    private readonly ILogger<BedrockChatModelStreaming> _logger;
    private readonly BedrockOptions _options;
    private readonly AmazonBedrockRuntimeClient _bedrockClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="BedrockChatModelStreaming"/> class.
    /// </summary>
    public BedrockChatModelStreaming(
        IOptions<BedrockOptions> options,
        ILogger<BedrockChatModelStreaming> logger)
    {
        _logger = logger;
        _options = options.Value;

        var config = new AmazonBedrockRuntimeConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_options.Region)
        };

        if (!string.IsNullOrEmpty(_options.AccessKeyId) && !string.IsNullOrEmpty(_options.SecretAccessKey))
        {
            var credentials = new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey);
            _bedrockClient = new AmazonBedrockRuntimeClient(credentials, config);
        }
        else
        {
            // Use default credentials (IAM role, environment variables, etc.)
            _bedrockClient = new AmazonBedrockRuntimeClient(config);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseChunk> GenerateStreamAsync(
        ChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var modelId = request.Metadata?.TryGetValue("modelId", out var modelIdValue) == true
            ? modelIdValue.ToString() ?? _options.DefaultModelId
            : _options.DefaultModelId;

        var systemMessages = request.Messages
            .Where(m => m.Role == MessageRole.System)
            .Select(m => m.Content)
            .ToList();
        var systemText = systemMessages.Count > 0 ? string.Join("\n", systemMessages) : null;

        var regularMessages = request.Messages
            .Where(m => m.Role != MessageRole.System)
            .ToList();

        var bedrockMessages = regularMessages.Select(m => new BedrockMessage(
            m.Role switch
            {
                MessageRole.User => "user",
                MessageRole.Assistant => "assistant",
                _ => "user"
            },
            new List<BedrockContentBlock>
            {
                new BedrockContentBlock("text", m.Content)
            })).ToList();

        List<BedrockTool>? bedrockTools = null;
        if (request.Tools != null && request.Tools.Count > 0)
        {
            bedrockTools = request.Tools.Select(t => new BedrockTool(
                t.Name,
                t.Description,
                t.Parameters)).ToList();
        }

        var maxTokens = request.MaxTokens ?? 4096;
        var bedrockRequest = new BedrockChatRequest(
            "bedrock-2023-05-31",
            maxTokens,
            bedrockMessages,
            systemText,
            request.Temperature,
            bedrockTools);

        var requestJson = JsonSerializer.Serialize(bedrockRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var invokeRequest = new InvokeModelWithResponseStreamRequest
        {
            ModelId = modelId,
            Body = new MemoryStream(Encoding.UTF8.GetBytes(requestJson)),
            ContentType = "application/json"
        };

        InvokeModelWithResponseStreamResponse? response = null;
        try
        {
            response = await _bedrockClient.InvokeModelWithResponseStreamAsync(invokeRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking Bedrock streaming for model {ModelId}", modelId);
            throw;
        }

        if (response == null || response.Body == null)
        {
            yield break;
        }

        Usage? finalUsage = null;
        string? finishReason = null;
        var accumulatedText = new StringBuilder();

        // Process stream events - ResponseStream needs to be converted to IAsyncEnumerable
        await foreach (var eventStream in response.Body.ToAsyncEnumerable())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            // Access the chunk via reflection since IEventStreamEvent interface doesn't expose it directly
            Stream? chunkStream = null;
            var eventType = eventStream.GetType();
            
            // Try to find Chunk property first (most common)
            var chunkProperty = eventType.GetProperty("Chunk", BindingFlags.Public | BindingFlags.Instance);
            if (chunkProperty != null)
            {
                var chunkValue = chunkProperty.GetValue(eventStream);
                if (chunkValue is Stream stream)
                {
                    chunkStream = stream;
                }
                else if (chunkValue is Task<Stream> chunkTask)
                {
                    chunkStream = await chunkTask;
                }
            }
            
            // If Chunk not found, try Payload
            if (chunkStream == null)
            {
                var payloadProperty = eventType.GetProperty("Payload", BindingFlags.Public | BindingFlags.Instance);
                if (payloadProperty != null)
                {
                    var payloadValue = payloadProperty.GetValue(eventStream);
                    if (payloadValue is Stream stream)
                    {
                        chunkStream = stream;
                    }
                    else if (payloadValue is Task<Stream> payloadTask)
                    {
                        chunkStream = await payloadTask;
                    }
                }
            }

            if (chunkStream == null || chunkStream.Length == 0)
            {
                continue;
            }
            if (chunkStream == null || chunkStream.Length == 0)
            {
                continue;
            }

            // Reset stream position to beginning (in case it was already read)
            chunkStream.Position = 0;

            // Read the chunk JSON from the stream
            string chunkJson;
            using (var reader = new StreamReader(chunkStream, Encoding.UTF8, leaveOpen: true))
            {
                chunkJson = await reader.ReadToEndAsync(cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(chunkJson))
            {
                continue;
            }

            ChatResponseChunk? chunkToYield = null;
            try
            {
                var streamChunk = JsonSerializer.Deserialize<BedrockStreamChunk>(
                    chunkJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (streamChunk == null)
                {
                    continue;
                }

                // Handle content delta chunks
                if (streamChunk.Type == "content_block_delta" && streamChunk.Delta != null)
                {
                    var deltaText = streamChunk.Delta.Text;
                    if (!string.IsNullOrEmpty(deltaText))
                    {
                        accumulatedText.Append(deltaText);
                        chunkToYield = new ChatResponseChunk(
                            deltaText,
                            IsComplete: false,
                            ModelId: modelId);
                    }
                }
                // Handle message stop event
                else if (streamChunk.Type == "message_stop")
                {
                    finishReason = "stop";
                }
                // Handle usage information
                else if (streamChunk.Usage != null)
                {
                    finalUsage = new Usage(
                        streamChunk.Usage.InputTokens,
                        0,
                        streamChunk.Usage.InputTokens + streamChunk.Usage.OutputTokens);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse Bedrock stream chunk: {ChunkJson}", chunkJson);
                continue;
            }

            if (chunkToYield != null)
            {
                yield return chunkToYield;
            }
        }

        // Yield final chunk with usage information
        if (finalUsage != null || finishReason != null)
        {
            yield return new ChatResponseChunk(
                string.Empty,
                IsComplete: true,
                ModelId: modelId,
                Usage: finalUsage,
                FinishReason: finishReason);
        }
    }

    /// <summary>
    /// Disposes the Bedrock client.
    /// </summary>
    public void Dispose()
    {
        _bedrockClient?.Dispose();
    }
}

/// <summary>
/// Internal DTO for Bedrock streaming chunk.
/// </summary>
internal record BedrockStreamChunk(
    [property: System.Text.Json.Serialization.JsonPropertyName("type")] string Type,
    [property: System.Text.Json.Serialization.JsonPropertyName("delta")] BedrockStreamDelta? Delta = null,
    [property: System.Text.Json.Serialization.JsonPropertyName("usage")] BedrockUsage? Usage = null);

/// <summary>
/// Internal DTO for Bedrock stream delta.
/// </summary>
internal record BedrockStreamDelta(
    [property: System.Text.Json.Serialization.JsonPropertyName("type")] string Type,
    [property: System.Text.Json.Serialization.JsonPropertyName("text")] string? Text = null);

