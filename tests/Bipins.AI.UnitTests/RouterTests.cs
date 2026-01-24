using Bipins.AI.Core.Models;
using Bipins.AI.Runtime.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests;

public class RouterTests
{
    [Fact]
    public async Task SelectChatModelAsync_WithRegisteredModel_ReturnsModel()
    {
        var services = new ServiceCollection();
        var chatModel = new Mock<IChatModel>();
        services.AddSingleton(chatModel.Object);

        var serviceProvider = services.BuildServiceProvider();
        var logger = new Mock<ILogger<DefaultModelRouter>>();
        var router = new DefaultModelRouter(logger.Object, serviceProvider);

        var request = new ChatRequest(new[] { new Message(MessageRole.User, "test") });
        var result = await router.SelectChatModelAsync("tenant1", request);

        Assert.NotNull(result);
        Assert.Same(chatModel.Object, result);
    }

    [Fact]
    public async Task SelectEmbeddingModelAsync_WithRegisteredModel_ReturnsModel()
    {
        var services = new ServiceCollection();
        var embeddingModel = new Mock<IEmbeddingModel>();
        services.AddSingleton(embeddingModel.Object);

        var serviceProvider = services.BuildServiceProvider();
        var logger = new Mock<ILogger<DefaultModelRouter>>();
        var router = new DefaultModelRouter(logger.Object, serviceProvider);

        var request = new EmbeddingRequest(new[] { "test" });
        var result = await router.SelectEmbeddingModelAsync("tenant1", request);

        Assert.NotNull(result);
        Assert.Same(embeddingModel.Object, result);
    }

    [Fact]
    public async Task SelectChatModelAsync_NoModels_ThrowsException()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var logger = new Mock<ILogger<DefaultModelRouter>>();
        var router = new DefaultModelRouter(logger.Object, serviceProvider);

        var request = new ChatRequest(new[] { new Message(MessageRole.User, "test") });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.SelectChatModelAsync("tenant1", request));
    }
}
