using Bipins.AI.Providers.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Providers;

/// <summary>
/// Integration-style tests for providers that test public APIs without accessing internal types.
/// </summary>
public class ProviderIntegrationTests
{
    [Fact]
    public void OpenAiChatModel_CanBeInstantiated()
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        var logger = new Mock<ILogger<OpenAiChatModel>>();
        var options = Options.Create(new OpenAiOptions
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.openai.com/v1"
        });

        var chatModel = new OpenAiChatModel(httpClientFactory.Object, options, logger.Object);

        Assert.NotNull(chatModel);
    }

    [Fact]
    public void OpenAiEmbeddingModel_CanBeInstantiated()
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        var logger = new Mock<ILogger<OpenAiEmbeddingModel>>();
        var options = Options.Create(new OpenAiOptions
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.openai.com/v1"
        });

        var embeddingModel = new OpenAiEmbeddingModel(httpClientFactory.Object, options, logger.Object);

        Assert.NotNull(embeddingModel);
    }
}
