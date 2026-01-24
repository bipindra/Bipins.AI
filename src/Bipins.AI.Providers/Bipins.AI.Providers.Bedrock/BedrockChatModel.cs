using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime;
using Bipins.AI.Core.Models;
using Bipins.AI.Providers.Bedrock.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Bipins.AI.Providers.Bedrock;

/// <summary>
/// AWS Bedrock implementation of IChatModel.
/// </summary>
public class BedrockChatModel : IChatModel
{
    private readonly ILogger<BedrockChatModel> _logger;
    private readonly BedrockOptions _options;
    private readonly AmazonBedrockRuntimeClient _bedrockClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="BedrockChatModel"/> class.
    /// </summary>
    public BedrockChatModel(
        IOptions<BedrockOptions> options,
        ILogger<BedrockChatModel> logger)
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
    public async Task<ChatResponse> GenerateAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var modelId = request.Metadata?.TryGetValue("modelId", out var modelIdValue) == true
            ? modelIdValue.ToString() ?? _options.DefaultModelId
            : _options.DefaultModelId;

        // Separate system messages
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

        // Map tools to Bedrock format
        List<BedrockTool>? bedrockTools = null;
        if (request.Tools != null && request.Tools.Count > 0)
        {
            bedrockTools = request.Tools.Select(t => new BedrockTool(
                t.Name,
                t.Description,
                t.Parameters)).ToList();
        }

        var maxTokens = request.MaxTokens ?? 4096;
        
        // Handle structured output (Bedrock/Anthropic supports JSON mode)
        object? responseFormat = null;
        if (request.StructuredOutput != null)
        {
            // Bedrock uses Anthropic format - JSON mode
            responseFormat = new { type = "json_object" };
        }
        
        var bedrockRequest = new BedrockChatRequest(
            "bedrock-2023-05-31", // Anthropic version for Bedrock
            maxTokens,
            bedrockMessages,
            systemText,
            request.Temperature,
            bedrockTools,
            responseFormat);

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

        var attempt = 0;
        while (attempt < _options.MaxRetries)
        {
            try
            {
                var response = await _bedrockClient.InvokeModelAsync(invokeRequest, cancellationToken);

                using var reader = new StreamReader(response.Body);
                var responseJson = await reader.ReadToEndAsync(cancellationToken);
                var bedrockResponse = JsonSerializer.Deserialize<BedrockChatResponse>(
                    responseJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (bedrockResponse == null || bedrockResponse.Content.Count == 0)
                {
                    throw new BedrockException("Empty response from Bedrock");
                }

                // Extract text content
                var textContent = string.Join("", bedrockResponse.Content
                    .Where(c => c.Type == "text")
                    .Select(c => c.Text ?? string.Empty));

                // Extract tool calls from tool_use content blocks
                var toolCalls = new List<ToolCall>();
                foreach (var contentBlock in bedrockResponse.Content)
                {
                    if (contentBlock.Type == "tool_use" && contentBlock.Id != null && contentBlock.Name != null)
                    {
                        System.Text.Json.JsonElement argumentsJson;
                        try
                        {
                            if (contentBlock.Input != null)
                            {
                                // Convert input object to JsonElement
                                var inputJson = JsonSerializer.Serialize(contentBlock.Input);
                                argumentsJson = JsonSerializer.Deserialize<System.Text.Json.JsonElement>(inputJson);
                            }
                            else
                            {
                                argumentsJson = JsonSerializer.SerializeToElement(new { });
                            }
                        }
                        catch
                        {
                            argumentsJson = JsonSerializer.SerializeToElement(new { });
                        }

                        toolCalls.Add(new ToolCall(
                            contentBlock.Id,
                            contentBlock.Name,
                            argumentsJson));
                    }
                }

                var usage = bedrockResponse.Usage != null
                    ? new Usage(
                        bedrockResponse.Usage.InputTokens,
                        0,
                        bedrockResponse.Usage.InputTokens + bedrockResponse.Usage.OutputTokens)
                    : null;

                return new ChatResponse(
                    textContent,
                    toolCalls.Count > 0 ? toolCalls : null,
                    usage,
                    modelId,
                    bedrockResponse.StopReason);

            }
            catch (Exception ex) when (IsRetryable(ex) && attempt < _options.MaxRetries - 1)
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

        throw new BedrockException("Failed to generate chat completion after retries");
    }

    private static bool IsRetryable(Exception ex)
    {
        // Retry on throttling or 5xx errors
        return ex.Message.Contains("Throttling") ||
               ex.Message.Contains("ServiceUnavailable") ||
               ex.Message.Contains("InternalServerError");
    }

    private static int CalculateBackoff(int attempt)
    {
        return (int)(Math.Pow(2, attempt) * 1000);
    }

    /// <summary>
    /// Disposes the Bedrock client.
    /// </summary>
    public void Dispose()
    {
        _bedrockClient?.Dispose();
    }
}

