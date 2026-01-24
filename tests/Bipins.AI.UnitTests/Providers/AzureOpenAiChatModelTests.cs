using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Bipins.AI.Core.Models;
using Bipins.AI.Providers.AzureOpenAI;
using Bipins.AI.Providers.AzureOpenAI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Bipins.AI.UnitTests.Providers;

public class AzureOpenAiChatModelTests
{
    private readonly Mock<ILogger<AzureOpenAiChatModel>> _logger;
    private readonly AzureOpenAiOptions _options;
    private readonly Mock<HttpMessageHandler> _httpMessageHandler;
    private readonly IHttpClientFactory _httpClientFactory;

    public AzureOpenAiChatModelTests()
    {
        _logger = new Mock<ILogger<AzureOpenAiChatModel>>();
        _options = new AzureOpenAiOptions
        {
            ApiKey = "test-api-key",
            Endpoint = "https://test.openai.azure.com",
            DefaultChatDeploymentName = "gpt-4",
            ApiVersion = "2024-02-15-preview",
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
        var responseContent = new AzureOpenAiChatResponse
        {
            Choices = new List<AzureOpenAiChoice>
            {
                new AzureOpenAiChoice
                {
                    Message = new AzureOpenAiChatMessage
                    {
                        Role = "assistant",
                        Content = "Hello, world!"
                    },
                    FinishReason = "stop"
                }
            },
            Model = "gpt-4",
            Usage = new AzureOpenAiUsage
            {
                PromptTokens = 10,
                CompletionTokens = 5,
                TotalTokens = 15
            }
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

        var chatModel = new AzureOpenAiChatModel(_httpClientFactory, Options.Create(_options), _logger.Object);
        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Hello")
        });

        // Act
        var result = await chatModel.GenerateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello, world!", result.Content);
        Assert.Equal("gpt-4", result.ModelId);
        Assert.NotNull(result.Usage);
        Assert.Equal(10, result.Usage.PromptTokens);
        Assert.Equal(5, result.Usage.CompletionTokens);
        Assert.Equal(15, result.Usage.TotalTokens);
    }

    [Fact]
    public async Task GenerateAsync_WithTools_IncludesToolsInRequest()
    {
        // Arrange
        var responseContent = new AzureOpenAiChatResponse
        {
            Choices = new List<AzureOpenAiChoice>
            {
                new AzureOpenAiChoice
                {
                    Message = new AzureOpenAiChatMessage
                    {
                        Role = "assistant",
                        Content = "Response"
                    },
                    FinishReason = "stop"
                }
            },
            Model = "gpt-4",
            Usage = new AzureOpenAiUsage { PromptTokens = 10, CompletionTokens = 5, TotalTokens = 15 }
        };

        var responseJson = JsonSerializer.Serialize(responseContent);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        AzureOpenAiChatRequest? capturedRequest = null;
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                var content = await req.Content!.ReadAsStringAsync(ct);
                capturedRequest = JsonSerializer.Deserialize<AzureOpenAiChatRequest>(content);
                return response;
            });

        var chatModel = new AzureOpenAiChatModel(_httpClientFactory, Options.Create(_options), _logger.Object);
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
        Assert.Equal("get_weather", capturedRequest.Tools[0].Function.Name);
    }

    [Fact]
    public async Task GenerateAsync_WithToolCalls_ReturnsToolCalls()
    {
        // Arrange
        var responseContent = new AzureOpenAiChatResponse
        {
            Choices = new List<AzureOpenAiChoice>
            {
                new AzureOpenAiChoice
                {
                    Message = new AzureOpenAiChatMessage
                    {
                        Role = "assistant",
                        Content = null,
                        ToolCalls = new List<AzureOpenAiToolCall>
                        {
                            new AzureOpenAiToolCall
                            {
                                Id = "call_123",
                                Function = new AzureOpenAiFunctionCall
                                {
                                    Name = "get_weather",
                                    Arguments = JsonSerializer.Serialize(new { location = "Seattle" })
                                }
                            }
                        }
                    },
                    FinishReason = "tool_calls"
                }
            },
            Model = "gpt-4",
            Usage = new AzureOpenAiUsage { PromptTokens = 10, CompletionTokens = 5, TotalTokens = 15 }
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

        var chatModel = new AzureOpenAiChatModel(_httpClientFactory, Options.Create(_options), _logger.Object);
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
        Assert.Equal("call_123", result.ToolCalls[0].Id);
        Assert.Equal("get_weather", result.ToolCalls[0].Name);
    }

    [Fact]
    public async Task GenerateAsync_WithCustomDeployment_UsesCustomDeployment()
    {
        // Arrange
        var responseContent = new AzureOpenAiChatResponse
        {
            Choices = new List<AzureOpenAiChoice>
            {
                new AzureOpenAiChoice
                {
                    Message = new AzureOpenAiChatMessage { Role = "assistant", Content = "Response" },
                    FinishReason = "stop"
                }
            },
            Model = "gpt-4-turbo",
            Usage = new AzureOpenAiUsage { PromptTokens = 10, CompletionTokens = 5, TotalTokens = 15 }
        };

        var responseJson = JsonSerializer.Serialize(responseContent);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        string? capturedUrl = null;
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                capturedUrl = req.RequestUri?.ToString();
                return Task.FromResult(response);
            });

        var chatModel = new AzureOpenAiChatModel(_httpClientFactory, Options.Create(_options), _logger.Object);
        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Hello")
        });
        request.Metadata = new Dictionary<string, object> { { "deploymentName", "gpt-4-turbo" } };

        // Act
        await chatModel.GenerateAsync(request);

        // Assert
        Assert.NotNull(capturedUrl);
        Assert.Contains("gpt-4-turbo", capturedUrl);
    }
}
