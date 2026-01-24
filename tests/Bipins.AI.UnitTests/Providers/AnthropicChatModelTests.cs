using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Bipins.AI.Core.Models;
using Bipins.AI.Providers.Anthropic;
using Bipins.AI.Providers.Anthropic.Models;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Bipins.AI.UnitTests.Providers;

public class AnthropicChatModelTests
{
    private readonly Mock<ILogger<AnthropicChatModel>> _logger;
    private readonly AnthropicOptions _options;
    private readonly Mock<HttpMessageHandler> _httpMessageHandler;
    private readonly IHttpClientFactory _httpClientFactory;

    public AnthropicChatModelTests()
    {
        _logger = new Mock<ILogger<AnthropicChatModel>>();
        _options = new AnthropicOptions
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://api.anthropic.com/v1",
            DefaultChatModelId = "claude-3-sonnet-20240229",
            ApiVersion = "2023-06-01",
            MaxRetries = 3,
            TimeoutSeconds = 30
        };

        _httpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandler.Object);
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
        _httpClientFactory = mockFactory.Object;
    }

    [Fact]
    public async Task GenerateAsync_WithValidRequest_ReturnsChatResponse()
    {
        // Arrange
        var responseContent = new AnthropicChatResponse
        {
            Content = new List<AnthropicContentBlock>
            {
                new AnthropicContentBlock { Type = "text", Text = "Hello, world!" }
            },
            Model = "claude-3-sonnet-20240229",
            StopReason = "end_turn",
            Usage = new AnthropicUsage { InputTokens = 10, OutputTokens = 5 }
        };

        var responseJson = JsonSerializer.Serialize(responseContent);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var chatModel = new AnthropicChatModel(_httpClientFactory, Options.Create(_options), _logger.Object);
        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Hello")
        });

        // Act
        var result = await chatModel.GenerateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello, world!", result.Content);
        Assert.Equal("claude-3-sonnet-20240229", result.ModelId);
        Assert.NotNull(result.Usage);
        Assert.Equal(10, result.Usage.PromptTokens);
        Assert.Equal(15, result.Usage.TotalTokens);
    }

    [Fact]
    public async Task GenerateAsync_WithSystemMessage_IncludesSystemInRequest()
    {
        // Arrange
        var responseContent = new AnthropicChatResponse
        {
            Content = new List<AnthropicContentBlock>
            {
                new AnthropicContentBlock { Type = "text", Text = "Response" }
            },
            Model = "claude-3-sonnet-20240229",
            StopReason = "end_turn",
            Usage = new AnthropicUsage { InputTokens = 10, OutputTokens = 5 }
        };

        var responseJson = JsonSerializer.Serialize(responseContent);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        AnthropicChatRequest? capturedRequest = null;
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                var content = await req.Content!.ReadAsStringAsync(ct);
                capturedRequest = JsonSerializer.Deserialize<AnthropicChatRequest>(content);
                return response;
            });

        var chatModel = new AnthropicChatModel(_httpClientFactory, Options.Create(_options), _logger.Object);
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
        var responseContent = new AnthropicChatResponse
        {
            Content = new List<AnthropicContentBlock>
            {
                new AnthropicContentBlock { Type = "text", Text = "Response" }
            },
            Model = "claude-3-sonnet-20240229",
            StopReason = "end_turn",
            Usage = new AnthropicUsage { InputTokens = 10, OutputTokens = 5 }
        };

        var responseJson = JsonSerializer.Serialize(responseContent);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        AnthropicChatRequest? capturedRequest = null;
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                var content = await req.Content!.ReadAsStringAsync(ct);
                capturedRequest = JsonSerializer.Deserialize<AnthropicChatRequest>(content);
                return response;
            });

        var chatModel = new AnthropicChatModel(_httpClientFactory, Options.Create(_options), _logger.Object);
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
    public async Task GenerateAsync_WithToolCalls_ReturnsToolCalls()
    {
        // Arrange
        var responseContent = new AnthropicChatResponse
        {
            Content = new List<AnthropicContentBlock>
            {
                new AnthropicContentBlock
                {
                    Type = "tool_use",
                    Id = "tool_123",
                    Name = "get_weather",
                    Input = new { location = "Seattle" }
                }
            },
            Model = "claude-3-sonnet-20240229",
            StopReason = "tool_use",
            Usage = new AnthropicUsage { InputTokens = 10, OutputTokens = 5 }
        };

        var responseJson = JsonSerializer.Serialize(responseContent);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var chatModel = new AnthropicChatModel(_httpClientFactory, Options.Create(_options), _logger.Object);
        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "What's the weather in Seattle?")
        });

        // Act
        var result = await chatModel.GenerateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ToolCalls);
        Assert.Single(result.ToolCalls);
        Assert.Equal("tool_123", result.ToolCalls[0].Id);
        Assert.Equal("get_weather", result.ToolCalls[0].Name);
    }

    [Fact]
    public async Task GenerateAsync_WithRateLimit_RetriesWithBackoff()
    {
        // Arrange
        var rateLimitResponse = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Headers = { RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(1)) }
        };

        var successResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new AnthropicChatResponse
            {
                Content = new List<AnthropicContentBlock>
                {
                    new AnthropicContentBlock { Type = "text", Text = "Success" }
                },
                Model = "claude-3-sonnet-20240229",
                StopReason = "end_turn",
                Usage = new AnthropicUsage { InputTokens = 10, OutputTokens = 5 }
            }), Encoding.UTF8, "application/json")
        };

        var callCount = 0;
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => callCount++ == 0 ? rateLimitResponse : successResponse);

        var chatModel = new AnthropicChatModel(_httpClientFactory, Options.Create(_options), _logger.Object);
        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Hello")
        });

        // Act
        var result = await chatModel.GenerateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Success", result.Content);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task GenerateAsync_WithCustomModelId_UsesCustomModel()
    {
        // Arrange
        var responseContent = new AnthropicChatResponse
        {
            Content = new List<AnthropicContentBlock>
            {
                new AnthropicContentBlock { Type = "text", Text = "Response" }
            },
            Model = "claude-3-opus-20240229",
            StopReason = "end_turn",
            Usage = new AnthropicUsage { InputTokens = 10, OutputTokens = 5 }
        };

        var responseJson = JsonSerializer.Serialize(responseContent);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        AnthropicChatRequest? capturedRequest = null;
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                var content = await req.Content!.ReadAsStringAsync(ct);
                capturedRequest = JsonSerializer.Deserialize<AnthropicChatRequest>(content);
                return response;
            });

        var chatModel = new AnthropicChatModel(_httpClientFactory, Options.Create(_options), _logger.Object);
        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Hello")
        });
        request.Metadata = new Dictionary<string, object> { { "modelId", "claude-3-opus-20240229" } };

        // Act
        await chatModel.GenerateAsync(request);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("claude-3-opus-20240229", capturedRequest.Model);
    }
}
