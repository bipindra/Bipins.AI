using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime;
using Bipins.AI.Core.Models;
using Bipins.AI.Connectors.Llm.Bedrock.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Bipins.AI.Connectors.Llm.Bedrock;

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
        // TODO: Bedrock streaming implementation needs AWS SDK ResponseStream enumeration
        // For now, fall back to non-streaming and yield the complete response as a single chunk
        _logger.LogWarning("Bedrock streaming not fully implemented, using non-streaming fallback");

        // Create a temporary BedrockChatModel instance for fallback
        var config = new AmazonBedrockRuntimeConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_options.Region)
        };

        AmazonBedrockRuntimeClient? bedrockClient = null;
        try
        {
            if (!string.IsNullOrEmpty(_options.AccessKeyId) && !string.IsNullOrEmpty(_options.SecretAccessKey))
            {
                var credentials = new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey);
                bedrockClient = new AmazonBedrockRuntimeClient(credentials, config);
            }
            else
            {
                bedrockClient = new AmazonBedrockRuntimeClient(config);
            }

            // Use the non-streaming implementation logic directly
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

            var invokeRequest = new InvokeModelRequest
            {
                ModelId = modelId,
                Body = new MemoryStream(Encoding.UTF8.GetBytes(requestJson)),
                ContentType = "application/json",
                Accept = "application/json"
            };

            var response = await bedrockClient.InvokeModelAsync(invokeRequest, cancellationToken);

            using var reader = new StreamReader(response.Body);
            var responseJson = await reader.ReadToEndAsync(cancellationToken);
            var bedrockResponse = JsonSerializer.Deserialize<BedrockChatResponse>(
                responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (bedrockResponse == null || bedrockResponse.Content.Count == 0)
            {
                yield break;
            }

            var textContent = string.Join("", bedrockResponse.Content
                .Where(c => c.Type == "text")
                .Select(c => c.Text ?? string.Empty));

            var usage = bedrockResponse.Usage != null
                ? new Usage(
                    bedrockResponse.Usage.InputTokens,
                    0,
                    bedrockResponse.Usage.InputTokens + bedrockResponse.Usage.OutputTokens)
                : null;

            // Yield the complete response as a single chunk
            yield return new ChatResponseChunk(
                textContent,
                IsComplete: true,
                ModelId: modelId,
                Usage: usage,
                FinishReason: bedrockResponse.StopReason);
        }
        finally
        {
            bedrockClient?.Dispose();
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
