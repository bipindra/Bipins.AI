using Bipins.AI.Core.Models;
using Bipins.AI.Providers;
using Bipins.AI.Safety.Middleware;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Safety;

public class ModeratedLLMProviderTests
{
    private readonly Mock<ILLMProvider> _mockInnerProvider;
    private readonly Mock<IChatModel> _mockChatModel;
    private readonly Mock<ILLMProviderMiddleware> _mockMiddleware;
    private readonly Mock<ILogger<ModeratedLLMProvider>> _mockLogger;
    private readonly ModeratedLLMProvider _provider;

    public ModeratedLLMProviderTests()
    {
        _mockInnerProvider = new Mock<ILLMProvider>();
        _mockChatModel = new Mock<IChatModel>();
        _mockMiddleware = new Mock<ILLMProviderMiddleware>();
        _mockLogger = new Mock<ILogger<ModeratedLLMProvider>>();

        _mockInnerProvider.Setup(p => p.CurrentModel).Returns(_mockChatModel.Object);

        _provider = new ModeratedLLMProvider(
            _mockInnerProvider.Object,
            new[] { _mockMiddleware.Object },
            _mockLogger.Object);
    }

    [Fact]
    public async Task ChatAsync_AppliesMiddlewareToRequestAndResponse()
    {
        var request = new ChatRequest(new[] { new Message(MessageRole.User, "Hello") });
        var modifiedRequest = request with { Temperature = 0.5f };
        var response = new ChatResponse("Hello!");
        var modifiedResponse = response with { Content = "Hello! Modified" };

        _mockMiddleware.Setup(m => m.OnRequestAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(modifiedRequest);
        _mockMiddleware.Setup(m => m.OnResponseAsync(modifiedRequest, response, It.IsAny<CancellationToken>()))
            .ReturnsAsync(modifiedResponse);
        _mockInnerProvider.Setup(p => p.ChatAsync(modifiedRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _provider.ChatAsync(request);

        Assert.Equal(modifiedResponse.Content, result.Content);
        _mockMiddleware.Verify(m => m.OnRequestAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockMiddleware.Verify(m => m.OnResponseAsync(modifiedRequest, response, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_AppliesMiddlewareToInput()
    {
        var input = "Original text";
        var modifiedInput = "Modified text";
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        _mockMiddleware.Setup(m => m.OnEmbeddingRequestAsync(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(modifiedInput);
        _mockInnerProvider.Setup(p => p.GenerateEmbeddingAsync(modifiedInput, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        var result = await _provider.GenerateEmbeddingAsync(input);

        Assert.Equal(embedding, result);
        _mockMiddleware.Verify(m => m.OnEmbeddingRequestAsync(input, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CurrentModel_ReturnsInnerProviderModel()
    {
        var model = _provider.CurrentModel;

        Assert.Equal(_mockChatModel.Object, model);
    }
}
