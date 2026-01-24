using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Bipins.AI.Core.Models;
using Bipins.AI.Providers.Bedrock;
using Bipins.AI.Providers.Bedrock.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Bipins.AI.UnitTests.Providers;

public class BedrockChatModelTests
{
    private readonly Mock<ILogger<BedrockChatModel>> _logger;
    private readonly BedrockOptions _options;
    private readonly Mock<IAmazonBedrockRuntime> _bedrockClient;

    public BedrockChatModelTests()
    {
        _logger = new Mock<ILogger<BedrockChatModel>>();
        _options = new BedrockOptions
        {
            Region = "us-east-1",
            DefaultModelId = "anthropic.claude-3-sonnet-20240229-v1:0",
            MaxRetries = 3
        };

        _bedrockClient = new Mock<IAmazonBedrockRuntime>();
    }

    [Fact]
    public async Task GenerateAsync_WithValidRequest_ReturnsChatResponse()
    {
        // Arrange
        var responseContent = new BedrockChatResponse
        {
            Content = new List<BedrockContentBlock>
            {
                new BedrockContentBlock { Type = "text", Text = "Hello, world!" }
            },
            StopReason = "end_turn",
            Usage = new BedrockUsage { InputTokens = 10, OutputTokens = 5 }
        };

        var responseJson = JsonSerializer.Serialize(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = new InvokeModelResponse
        {
            Body = new MemoryStream(Encoding.UTF8.GetBytes(responseJson)),
            ContentType = "application/json"
        };

        _bedrockClient
            .Setup(x => x.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var chatModel = new BedrockChatModel(Options.Create(_options), _logger.Object);
        // Use reflection to set the private _bedrockClient field for testing
        var field = typeof(BedrockChatModel).GetField("_bedrockClient", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(chatModel, _bedrockClient.Object);
        }

        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Hello")
        });

        // Act
        var result = await chatModel.GenerateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello, world!", result.Content);
        Assert.Equal("anthropic.claude-3-sonnet-20240229-v1:0", result.ModelId);
        Assert.NotNull(result.Usage);
        Assert.Equal(10, result.Usage.PromptTokens);
        Assert.Equal(15, result.Usage.TotalTokens);
    }

    [Fact]
    public async Task GenerateAsync_WithSystemMessage_IncludesSystemInRequest()
    {
        // Arrange
        var responseContent = new BedrockChatResponse
        {
            Content = new List<BedrockContentBlock>
            {
                new BedrockContentBlock { Type = "text", Text = "Response" }
            },
            StopReason = "end_turn",
            Usage = new BedrockUsage { InputTokens = 10, OutputTokens = 5 }
        };

        var responseJson = JsonSerializer.Serialize(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = new InvokeModelResponse
        {
            Body = new MemoryStream(Encoding.UTF8.GetBytes(responseJson)),
            ContentType = "application/json"
        };

        BedrockChatRequest? capturedRequest = null;
        _bedrockClient
            .Setup(x => x.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
            .Returns<InvokeModelRequest, CancellationToken>(async (req, ct) =>
            {
                using var reader = new StreamReader(req.Body);
                var body = await reader.ReadToEndAsync(ct);
                capturedRequest = JsonSerializer.Deserialize<BedrockChatRequest>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return response;
            });

        var chatModel = new BedrockChatModel(Options.Create(_options), _logger.Object);
        var field = typeof(BedrockChatModel).GetField("_bedrockClient", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(chatModel, _bedrockClient.Object);
        }

        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.System, "You are a helpful assistant"),
            new Message(MessageRole.User, "Hello")
        });

        // Act
        await chatModel.GenerateAsync(request);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("You are a helpful assistant", capturedRequest.System);
    }

    [Fact]
    public async Task GenerateAsync_WithTools_IncludesToolsInRequest()
    {
        // Arrange
        var responseContent = new BedrockChatResponse
        {
            Content = new List<BedrockContentBlock>
            {
                new BedrockContentBlock { Type = "text", Text = "Response" }
            },
            StopReason = "end_turn",
            Usage = new BedrockUsage { InputTokens = 10, OutputTokens = 5 }
        };

        var responseJson = JsonSerializer.Serialize(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = new InvokeModelResponse
        {
            Body = new MemoryStream(Encoding.UTF8.GetBytes(responseJson)),
            ContentType = "application/json"
        };

        BedrockChatRequest? capturedRequest = null;
        _bedrockClient
            .Setup(x => x.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
            .Returns<InvokeModelRequest, CancellationToken>(async (req, ct) =>
            {
                using var reader = new StreamReader(req.Body);
                var body = await reader.ReadToEndAsync(ct);
                capturedRequest = JsonSerializer.Deserialize<BedrockChatRequest>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return response;
            });

        var chatModel = new BedrockChatModel(Options.Create(_options), _logger.Object);
        var field = typeof(BedrockChatModel).GetField("_bedrockClient", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(chatModel, _bedrockClient.Object);
        }

        var tools = new List<ToolDefinition>
        {
            new ToolDefinition("get_weather", "Get weather", JsonSerializer.SerializeToElement(new { type = "object" }))
        };
        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "What's the weather?")
        }, Tools: tools);

        // Act
        await chatModel.GenerateAsync(request);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequest.Tools);
        Assert.Single(capturedRequest.Tools);
        Assert.Equal("get_weather", capturedRequest.Tools[0].Name);
    }

    [Fact]
    public async Task GenerateAsync_WithCustomModelId_UsesCustomModel()
    {
        // Arrange
        var responseContent = new BedrockChatResponse
        {
            Content = new List<BedrockContentBlock>
            {
                new BedrockContentBlock { Type = "text", Text = "Response" }
            },
            StopReason = "end_turn",
            Usage = new BedrockUsage { InputTokens = 10, OutputTokens = 5 }
        };

        var responseJson = JsonSerializer.Serialize(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = new InvokeModelResponse
        {
            Body = new MemoryStream(Encoding.UTF8.GetBytes(responseJson)),
            ContentType = "application/json"
        };

        string? capturedModelId = null;
        _bedrockClient
            .Setup(x => x.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), It.IsAny<CancellationToken>()))
            .Returns<InvokeModelRequest, CancellationToken>((req, ct) =>
            {
                capturedModelId = req.ModelId;
                return Task.FromResult(response);
            });

        var chatModel = new BedrockChatModel(Options.Create(_options), _logger.Object);
        var field = typeof(BedrockChatModel).GetField("_bedrockClient", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(chatModel, _bedrockClient.Object);
        }

        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Hello")
        });
        request.Metadata = new Dictionary<string, object> { { "modelId", "anthropic.claude-3-opus-20240229-v1:0" } };

        // Act
        await chatModel.GenerateAsync(request);

        // Assert
        Assert.NotNull(capturedModelId);
        Assert.Equal("anthropic.claude-3-opus-20240229-v1:0", capturedModelId);
    }
}
