using Bipins.AI.Core.Models;
using Bipins.AI.Providers;
using Bipins.AI.Providers.Anthropic;
using Bipins.AI.Providers.AzureOpenAI;
using Bipins.AI.Providers.Bedrock;
using Bipins.AI.Providers.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Xunit;

namespace Bipins.AI.UnitTests.Providers;

public class LLMProviderTests
{
    private readonly Mock<IChatModel> _mockChatModel;
    private readonly Mock<IChatModelStreaming> _mockChatModelStreaming;
    private readonly Mock<IEmbeddingModel> _mockEmbeddingModel;
    private readonly Mock<ILogger<OpenAiLLMProvider>> _mockLogger;

    public LLMProviderTests()
    {
        _mockChatModel = new Mock<IChatModel>();
        _mockChatModelStreaming = new Mock<IChatModelStreaming>();
        _mockEmbeddingModel = new Mock<IEmbeddingModel>();
        _mockLogger = new Mock<ILogger<OpenAiLLMProvider>>();
    }

    [Fact]
    public void OpenAiLLMProvider_CurrentModel_ReturnsChatModel()
    {
        var options = Options.Create(new OpenAiOptions { ApiKey = "test" });
        var provider = new OpenAiLLMProvider(
            _mockChatModel.Object,
            _mockChatModelStreaming.Object,
            _mockEmbeddingModel.Object,
            options,
            _mockLogger.Object);

        Assert.Same(_mockChatModel.Object, provider.CurrentModel);
    }

    [Fact]
    public async Task OpenAiLLMProvider_ChatAsync_DelegatesToChatModel()
    {
        var options = Options.Create(new OpenAiOptions { ApiKey = "test" });
        var provider = new OpenAiLLMProvider(
            _mockChatModel.Object,
            _mockChatModelStreaming.Object,
            _mockEmbeddingModel.Object,
            options,
            _mockLogger.Object);

        var request = new ChatRequest(new[] { new Message(MessageRole.User, "Hello") });
        var expectedResponse = new ChatResponse("Hi there");

        _mockChatModel.Setup(m => m.GenerateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await provider.ChatAsync(request);

        Assert.Same(expectedResponse, result);
        _mockChatModel.Verify(m => m.GenerateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OpenAiLLMProvider_ChatStreamAsync_DelegatesToChatModelStreaming()
    {
        var options = Options.Create(new OpenAiOptions { ApiKey = "test" });
        var provider = new OpenAiLLMProvider(
            _mockChatModel.Object,
            _mockChatModelStreaming.Object,
            _mockEmbeddingModel.Object,
            options,
            _mockLogger.Object);

        var request = new ChatRequest(new[] { new Message(MessageRole.User, "Hello") });
        async IAsyncEnumerable<ChatResponseChunk> GetChunks()
        {
            yield return new ChatResponseChunk("Hello", false);
            yield return new ChatResponseChunk(" there", true);
        }
        var chunks = GetChunks();

        _mockChatModelStreaming.Setup(m => m.GenerateStreamAsync(request, It.IsAny<CancellationToken>()))
            .Returns(chunks);

        var result = provider.ChatStreamAsync(request);
        var resultList = new List<ChatResponseChunk>();
        await foreach (var chunk in result)
        {
            resultList.Add(chunk);
        }

        Assert.Equal(2, resultList.Count);
        Assert.Equal("Hello", resultList[0].Content);
        Assert.Equal(" there", resultList[1].Content);
    }

    [Fact]
    public async Task OpenAiLLMProvider_GenerateEmbeddingAsync_DelegatesToEmbeddingModel()
    {
        var options = Options.Create(new OpenAiOptions 
        { 
            ApiKey = "test",
            DefaultEmbeddingModelId = "text-embedding-ada-002"
        });
        var provider = new OpenAiLLMProvider(
            _mockChatModel.Object,
            _mockChatModelStreaming.Object,
            _mockEmbeddingModel.Object,
            options,
            _mockLogger.Object);

        var expectedVector = new float[] { 0.1f, 0.2f, 0.3f };
        var embeddingResponse = new EmbeddingResponse(
            new[] { new ReadOnlyMemory<float>(expectedVector) },
            ModelId: "text-embedding-ada-002");

        _mockEmbeddingModel.Setup(m => m.EmbedAsync(
                It.Is<EmbeddingRequest>(r => r.Inputs.Count == 1 && r.Inputs[0] == "test"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingResponse);

        var result = await provider.GenerateEmbeddingAsync("test");

        Assert.Equal(expectedVector, result);
    }

    [Fact]
    public async Task OpenAiLLMProvider_GenerateEmbeddingAsync_NoVectors_ThrowsException()
    {
        var options = Options.Create(new OpenAiOptions 
        { 
            ApiKey = "test",
            DefaultEmbeddingModelId = "text-embedding-ada-002"
        });
        var provider = new OpenAiLLMProvider(
            _mockChatModel.Object,
            _mockChatModelStreaming.Object,
            _mockEmbeddingModel.Object,
            options,
            _mockLogger.Object);

        var embeddingResponse = new EmbeddingResponse(
            Array.Empty<ReadOnlyMemory<float>>(),
            ModelId: "text-embedding-ada-002");

        _mockEmbeddingModel.Setup(m => m.EmbedAsync(
                It.IsAny<EmbeddingRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingResponse);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GenerateEmbeddingAsync("test"));
    }

    [Fact]
    public async Task AnthropicLLMProvider_ChatAsync_DelegatesToChatModel()
    {
        var mockLogger = new Mock<ILogger<AnthropicLLMProvider>>();
        var options = Options.Create(new AnthropicOptions { ApiKey = "test" });
        var provider = new AnthropicLLMProvider(
            _mockChatModel.Object,
            _mockChatModelStreaming.Object,
            options,
            mockLogger.Object);

        var request = new ChatRequest(new[] { new Message(MessageRole.User, "Hello") });
        var expectedResponse = new ChatResponse("Hi there");

        _mockChatModel.Setup(m => m.GenerateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await provider.ChatAsync(request);

        Assert.Same(expectedResponse, result);
    }

    [Fact]
    public async Task AnthropicLLMProvider_GenerateEmbeddingAsync_ThrowsNotSupportedException()
    {
        var mockLogger = new Mock<ILogger<AnthropicLLMProvider>>();
        var options = Options.Create(new AnthropicOptions { ApiKey = "test" });
        var provider = new AnthropicLLMProvider(
            _mockChatModel.Object,
            _mockChatModelStreaming.Object,
            options,
            mockLogger.Object);

        await Assert.ThrowsAsync<NotSupportedException>(() => provider.GenerateEmbeddingAsync("test"));
    }

    [Fact]
    public async Task AzureOpenAiLLMProvider_ChatAsync_DelegatesToChatModel()
    {
        var mockLogger = new Mock<ILogger<AzureOpenAiLLMProvider>>();
        var options = Options.Create(new AzureOpenAiOptions 
        { 
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "test"
        });
        var provider = new AzureOpenAiLLMProvider(
            _mockChatModel.Object,
            _mockChatModelStreaming.Object,
            _mockEmbeddingModel.Object,
            options,
            mockLogger.Object);

        var request = new ChatRequest(new[] { new Message(MessageRole.User, "Hello") });
        var expectedResponse = new ChatResponse("Hi there");

        _mockChatModel.Setup(m => m.GenerateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await provider.ChatAsync(request);

        Assert.Same(expectedResponse, result);
    }

    [Fact]
    public async Task BedrockLLMProvider_ChatAsync_DelegatesToChatModel()
    {
        var mockLogger = new Mock<ILogger<BedrockLLMProvider>>();
        var options = Options.Create(new BedrockOptions { Region = "us-east-1" });
        var provider = new BedrockLLMProvider(
            _mockChatModel.Object,
            _mockChatModelStreaming.Object,
            options,
            mockLogger.Object);

        var request = new ChatRequest(new[] { new Message(MessageRole.User, "Hello") });
        var expectedResponse = new ChatResponse("Hi there");

        _mockChatModel.Setup(m => m.GenerateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await provider.ChatAsync(request);

        Assert.Same(expectedResponse, result);
    }

    [Fact]
    public async Task BedrockLLMProvider_GenerateEmbeddingAsync_ThrowsNotSupportedException()
    {
        var mockLogger = new Mock<ILogger<BedrockLLMProvider>>();
        var options = Options.Create(new BedrockOptions { Region = "us-east-1" });
        var provider = new BedrockLLMProvider(
            _mockChatModel.Object,
            _mockChatModelStreaming.Object,
            options,
            mockLogger.Object);

        await Assert.ThrowsAsync<NotSupportedException>(() => provider.GenerateEmbeddingAsync("test"));
    }
}
