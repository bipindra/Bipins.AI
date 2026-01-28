using Bipins.AI.Core.Models;
using Bipins.AI.Providers.Anthropic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bipins.AI.IntegrationTests.Providers;

[Collection("Integration")]
public class AnthropicIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public AnthropicIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Requires Anthropic API key")]
    public async Task AnthropicChatModel_GenerateAsync_ReturnsResponse()
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            return; // Skip if no API key
        }

        var httpClientFactory = _fixture.Services.GetRequiredService<IHttpClientFactory>();
        var logger = _fixture.Services.GetRequiredService<ILogger<AnthropicChatModel>>();
        var options = Options.Create(new AnthropicOptions
        {
            ApiKey = apiKey,
            DefaultChatModelId = "claude-3-haiku-20240307"
        });

        var chatModel = new AnthropicChatModel(httpClientFactory, options, logger);

        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Say 'Hello, World!' and nothing else.")
        });

        var response = await chatModel.GenerateAsync(request);

        Assert.NotNull(response);
        Assert.NotNull(response.Content);
        Assert.Contains("Hello", response.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Skip = "Requires Anthropic API key")]
    public async Task AnthropicChatModel_WithTools_ReturnsToolCalls()
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            return;
        }

        var httpClientFactory = _fixture.Services.GetRequiredService<IHttpClientFactory>();
        var logger = _fixture.Services.GetRequiredService<ILogger<AnthropicChatModel>>();
        var options = Options.Create(new AnthropicOptions
        {
            ApiKey = apiKey,
            DefaultChatModelId = "claude-3-haiku-20240307"
        });

        var chatModel = new AnthropicChatModel(httpClientFactory, options, logger);

        var tools = new List<ToolDefinition>
        {
            new ToolDefinition(
                "get_weather",
                "Get the current weather",
                System.Text.Json.JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new
                    {
                        location = new { type = "string", description = "City name" }
                    },
                    required = new[] { "location" }
                }))
        };

        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "What's the weather in Seattle?")
        }, Tools: tools);

        var response = await chatModel.GenerateAsync(request);

        Assert.NotNull(response);
        // May or may not have tool calls depending on model behavior
        if (response.ToolCalls != null && response.ToolCalls.Count > 0)
        {
            Assert.Single(response.ToolCalls);
            Assert.Equal("get_weather", response.ToolCalls[0].Name);
        }
    }

    [Fact(Skip = "Requires Anthropic API key")]
    public async Task AnthropicLLMProvider_ChatAsync_ReturnsResponse()
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            return;
        }

        var httpClientFactory = _fixture.Services.GetRequiredService<IHttpClientFactory>();
        var chatLogger = _fixture.Services.GetRequiredService<ILogger<AnthropicChatModel>>();
        var streamingLogger = _fixture.Services.GetRequiredService<ILogger<AnthropicChatModelStreaming>>();
        var providerLogger = _fixture.Services.GetRequiredService<ILogger<AnthropicLLMProvider>>();

        var options = Options.Create(new AnthropicOptions
        {
            ApiKey = apiKey,
            DefaultChatModelId = "claude-3-haiku-20240307"
        });

        var chatModel = new AnthropicChatModel(httpClientFactory, options, chatLogger);
        var streamingModel = new AnthropicChatModelStreaming(httpClientFactory, options, streamingLogger);

        var provider = new AnthropicLLMProvider(chatModel, streamingModel, options, providerLogger);

        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Say 'Hello from Anthropic LLM provider' and nothing else.")
        });

        var response = await provider.ChatAsync(request);

        Assert.NotNull(response);
        Assert.NotNull(response.Content);
        Assert.Contains("Hello", response.Content, StringComparison.OrdinalIgnoreCase);
    }
}
