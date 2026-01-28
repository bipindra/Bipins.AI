using Bipins.AI.Core.Models;
using Bipins.AI.LLM;
using Bipins.AI.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace Bipins.AI.UnitTests.Providers;

public class ChatServiceTests
{
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<ILogger<ChatService>> _mockLogger;
    private readonly ChatServiceOptions _options;
    private readonly ChatService _chatService;

    public ChatServiceTests()
    {
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockLogger = new Mock<ILogger<ChatService>>();
        _options = new ChatServiceOptions
        {
            Model = "gpt-4",
            Temperature = 0.7,
            MaxTokens = 2000
        };
        _chatService = new ChatService(_mockLLMProvider.Object, _options, _mockLogger.Object);
    }

    [Fact]
    public async Task ChatAsync_WithSystemAndUserPrompt_ReturnsContent()
    {
        var expectedResponse = new ChatResponse("Hello! How can I help you?");
        
        _mockLLMProvider.Setup(p => p.ChatAsync(
                It.Is<ChatRequest>(r => 
                    r.Messages.Count == 2 &&
                    r.Messages[0].Role == MessageRole.System &&
                    r.Messages[0].Content == "You are a helpful assistant" &&
                    r.Messages[1].Role == MessageRole.User &&
                    r.Messages[1].Content == "Hello" &&
                    r.Temperature == 0.7f &&
                    r.MaxTokens == 2000),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _chatService.ChatAsync("You are a helpful assistant", "Hello");

        Assert.Equal("Hello! How can I help you?", result);
    }

    [Fact]
    public async Task ChatAsync_WithModelInOptions_SetsMetadata()
    {
        _options.Model = "gpt-3.5-turbo";
        var chatService = new ChatService(_mockLLMProvider.Object, _options, _mockLogger.Object);
        
        var expectedResponse = new ChatResponse("Response");
        
        _mockLLMProvider.Setup(p => p.ChatAsync(
                It.Is<ChatRequest>(r => 
                    r.Metadata != null &&
                    r.Metadata.ContainsKey("modelId") &&
                    r.Metadata["modelId"].ToString() == "gpt-3.5-turbo"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        await chatService.ChatAsync("System", "User");
        
        _mockLLMProvider.Verify(p => p.ChatAsync(
            It.Is<ChatRequest>(r => r.Metadata != null && r.Metadata.ContainsKey("modelId")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChatWithToolsAsync_WithTools_PassesToolsToRequest()
    {
        var tools = new List<ToolDefinition>
        {
            new ToolDefinition(
                "get_weather",
                "Get weather for a location",
                JsonSerializer.SerializeToElement(new { type = "object", properties = new { } }))
        };

        var expectedResponse = new ChatResponse("I'll get the weather for you.");
        
        _mockLLMProvider.Setup(p => p.ChatAsync(
                It.Is<ChatRequest>(r => 
                    r.Tools != null &&
                    r.Tools.Count == 1 &&
                    r.Tools[0].Name == "get_weather"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _chatService.ChatWithToolsAsync("System", "Get weather", tools);

        Assert.NotNull(result);
        Assert.Equal("I'll get the weather for you.", result.Content);
    }

    [Fact]
    public async Task ChatWithToolsAsync_WithoutTools_PassesNullTools()
    {
        var expectedResponse = new ChatResponse("Response");
        
        _mockLLMProvider.Setup(p => p.ChatAsync(
                It.Is<ChatRequest>(r => r.Tools == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _chatService.ChatWithToolsAsync("System", "User", null);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ChatStreamAsync_ReturnsStreamingChunks()
    {
        async IAsyncEnumerable<ChatResponseChunk> GetChunks()
        {
            yield return new ChatResponseChunk("Hello", false);
            yield return new ChatResponseChunk(" there", false);
            yield return new ChatResponseChunk("!", true);
        }
        var chunks = GetChunks();

        _mockLLMProvider.Setup(p => p.ChatStreamAsync(
                It.Is<ChatRequest>(r => 
                    r.Messages.Count == 2 &&
                    r.Messages[0].Role == MessageRole.System &&
                    r.Messages[1].Role == MessageRole.User),
                It.IsAny<CancellationToken>()))
            .Returns(chunks);

        var result = _chatService.ChatStreamAsync("System", "Hello");
        var resultList = new List<ChatResponseChunk>();
        await foreach (var chunk in result)
        {
            resultList.Add(chunk);
        }

        Assert.Equal(3, resultList.Count);
        Assert.Equal("Hello", resultList[0].Content);
        Assert.Equal(" there", resultList[1].Content);
        Assert.Equal("!", resultList[2].Content);
        Assert.True(resultList[2].IsComplete);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_DelegatesToProvider()
    {
        var expectedVector = new float[] { 0.1f, 0.2f, 0.3f };
        
        _mockLLMProvider.Setup(p => p.GenerateEmbeddingAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedVector);

        var result = await _chatService.GenerateEmbeddingAsync("test");

        Assert.Equal(expectedVector, result);
        _mockLLMProvider.Verify(p => p.GenerateEmbeddingAsync("test", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ChatServiceOptions_DefaultValues_AreSet()
    {
        var options = new ChatServiceOptions();

        Assert.Equal(0.7, options.Temperature);
        Assert.Equal(2000, options.MaxTokens);
        Assert.Equal("text-embedding-3-small", options.EmbeddingModel);
        Assert.Equal(string.Empty, options.Model);
    }
}
