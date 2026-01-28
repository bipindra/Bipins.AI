using Bipins.AI.Core.Models;
using Bipins.AI.Providers.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bipins.AI.IntegrationTests.Providers;

[Collection("Integration")]
public class OpenAiIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public OpenAiIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task OpenAiChatModel_GenerateAsync_ReturnsResponse()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com/v1";
        var modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

        if (string.IsNullOrEmpty(apiKey))
        {
            return; // Skip if no API key
        }

        var httpClientFactory = _fixture.Services.GetRequiredService<IHttpClientFactory>();
        var logger = _fixture.Services.GetRequiredService<ILogger<OpenAiChatModel>>();
        var options = Options.Create(new OpenAiOptions
        {
            ApiKey = apiKey,
            BaseUrl = baseUrl,
            DefaultChatModelId = modelId
        });

        var chatModel = new OpenAiChatModel(httpClientFactory, options, logger);

        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Say 'Hello, World!' and nothing else.")
        });

        var response = await chatModel.GenerateAsync(request);

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Content));
        Assert.Contains("Hello", response.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenAiEmbeddingModel_EmbedAsync_ReturnsVector()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com/v1";
        var embeddingModelId = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL") ?? "text-embedding-3-small";

        if (string.IsNullOrEmpty(apiKey))
        {
            return; // Skip if no API key
        }

        var httpClientFactory = _fixture.Services.GetRequiredService<IHttpClientFactory>();
        var logger = _fixture.Services.GetRequiredService<ILogger<OpenAiEmbeddingModel>>();
        var options = Options.Create(new OpenAiOptions
        {
            ApiKey = apiKey,
            BaseUrl = baseUrl,
            DefaultEmbeddingModelId = embeddingModelId
        });

        var embeddingModel = new OpenAiEmbeddingModel(httpClientFactory, options, logger);

        var request = new EmbeddingRequest(new[] { "integration test input" });

        var response = await embeddingModel.EmbedAsync(request);

        Assert.NotNull(response);
        Assert.NotNull(response.Vectors);
        Assert.Single(response.Vectors);
        Assert.True(response.Vectors[0].Length > 0);
    }

    [Fact]
    public async Task OpenAiChatModelStreaming_GenerateStreamAsync_ReturnsChunks()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com/v1";
        var modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

        if (string.IsNullOrEmpty(apiKey))
        {
            return; // Skip if no API key
        }

        var httpClientFactory = _fixture.Services.GetRequiredService<IHttpClientFactory>();
        var logger = _fixture.Services.GetRequiredService<ILogger<OpenAiChatModelStreaming>>();
        var options = Options.Create(new OpenAiOptions
        {
            ApiKey = apiKey,
            BaseUrl = baseUrl,
            DefaultChatModelId = modelId
        });

        var streamingModel = new OpenAiChatModelStreaming(httpClientFactory, options, logger);

        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Stream the phrase 'Hello streaming' in a few chunks.")
        });

        var chunks = new List<ChatResponseChunk>();
        await foreach (var chunk in streamingModel.GenerateStreamAsync(request))
        {
            chunks.Add(chunk);
        }

        Assert.NotEmpty(chunks);
        Assert.Contains(chunks, c => !string.IsNullOrEmpty(c.Content));
        Assert.Contains(chunks, c => c.IsComplete);
    }

    [Fact]
    public async Task OpenAiLLMProvider_ChatAsync_ReturnsResponse()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com/v1";
        var modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";
        var embeddingModelId = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL") ?? "text-embedding-3-small";

        if (string.IsNullOrEmpty(apiKey))
        {
            return; // Skip if no API key
        }

        var httpClientFactory = _fixture.Services.GetRequiredService<IHttpClientFactory>();
        var chatLogger = _fixture.Services.GetRequiredService<ILogger<OpenAiChatModel>>();
        var streamingLogger = _fixture.Services.GetRequiredService<ILogger<OpenAiChatModelStreaming>>();
        var embeddingLogger = _fixture.Services.GetRequiredService<ILogger<OpenAiEmbeddingModel>>();
        var providerLogger = _fixture.Services.GetRequiredService<ILogger<OpenAiLLMProvider>>();

        var options = Options.Create(new OpenAiOptions
        {
            ApiKey = apiKey,
            BaseUrl = baseUrl,
            DefaultChatModelId = modelId,
            DefaultEmbeddingModelId = embeddingModelId
        });

        var chatModel = new OpenAiChatModel(httpClientFactory, options, chatLogger);
        var streamingModel = new OpenAiChatModelStreaming(httpClientFactory, options, streamingLogger);
        var embeddingModel = new OpenAiEmbeddingModel(httpClientFactory, options, embeddingLogger);

        var provider = new OpenAiLLMProvider(chatModel, streamingModel, embeddingModel, options, providerLogger);

        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Say 'Hello from provider' and nothing else.")
        });

        var response = await provider.ChatAsync(request);

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Content));
        Assert.Contains("Hello", response.Content, StringComparison.OrdinalIgnoreCase);
    }
}

