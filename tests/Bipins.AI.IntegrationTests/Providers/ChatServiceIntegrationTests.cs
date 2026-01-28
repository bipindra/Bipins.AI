using Bipins.AI.Core.Models;
using Bipins.AI.LLM;
using Bipins.AI.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bipins.AI.IntegrationTests.Providers;

[Collection("Integration")]
public class ChatServiceIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public ChatServiceIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Requires OpenAI API key")]
    public async Task ChatService_WithOpenAiProvider_ChatAsync_ReturnsContent()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            return; // Skip if not configured
        }

        // Resolve the OpenAI-backed ILLMProvider from DI
        var llmProvider = _fixture.Services.GetRequiredService<ILLMProvider>();
        var logger = _fixture.Services.GetRequiredService<ILogger<ChatService>>();

        var options = new ChatServiceOptions
        {
            Model = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini",
            Temperature = 0.7,
            MaxTokens = 200
        };

        var chatService = new ChatService(llmProvider, options, logger);

        var result = await chatService.ChatAsync(
            "You are a helpful assistant.",
            "Say 'Hello from ChatService' and nothing else.");

        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.Contains("Hello", result, StringComparison.OrdinalIgnoreCase);
    }
}

