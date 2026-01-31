using Bipins.AI.Core.Models;
using Bipins.AI.Providers;
using Bipins.AI.Providers.OpenAI;
using Bipins.AI.Safety;
using Bipins.AI.Safety.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bipins.AI.IntegrationTests.Safety;

[Collection("Integration")]
public class ContentModerationIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public ContentModerationIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Requires OpenAI API key and Azure Content Moderator")]
    public async Task ModeratedLLMProvider_WithOpenAI_AppliesContentModeration()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            return;
        }

        // Setup OpenAI provider
        var httpClientFactory = _fixture.Services.GetRequiredService<IHttpClientFactory>();
        var logger = _fixture.Services.GetRequiredService<ILogger<OpenAiLLMProvider>>();
        var openAiOptions = Options.Create(new OpenAiOptions
        {
            ApiKey = apiKey,
            BaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com/v1",
            DefaultChatModelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini"
        });

        var chatModel = new OpenAiChatModel(httpClientFactory, openAiOptions, _fixture.Services.GetRequiredService<ILogger<OpenAiChatModel>>());
        var chatModelStreaming = new OpenAiChatModelStreaming(httpClientFactory, openAiOptions, _fixture.Services.GetRequiredService<ILogger<OpenAiChatModelStreaming>>());
        var embeddingModel = new OpenAiEmbeddingModel(httpClientFactory, openAiOptions, _fixture.Services.GetRequiredService<ILogger<OpenAiEmbeddingModel>>());

        var openAiProvider = new OpenAiLLMProvider(chatModel, chatModelStreaming, embeddingModel, openAiOptions, logger);

        // Setup content moderation (using a mock for now since Azure Content Moderator requires subscription)
        var mockModerator = new MockContentModerator();
        var moderationOptions = Options.Create(new ContentModerationOptions
        {
            Enabled = true,
            MinimumSeverityToBlock = SafetySeverity.High,
            FilterUnsafeContent = false
        });
        var moderationLogger = _fixture.Services.GetRequiredService<ILogger<ContentModerationLLMMiddleware>>();
        var moderationMiddleware = new ContentModerationLLMMiddleware(mockModerator, moderationOptions, moderationLogger);

        // Wrap provider with moderation
        var moderatedProvider = new ModeratedLLMProvider(
            openAiProvider,
            new[] { moderationMiddleware },
            _fixture.Services.GetRequiredService<ILogger<ModeratedLLMProvider>>());

        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Say 'Hello, World!' and nothing else.")
        });

        var response = await moderatedProvider.ChatAsync(request);

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Content));
        Assert.NotNull(response.Safety);
    }
}

// Mock content moderator for testing
internal class MockContentModerator : IContentModerator
{
    public Task<ModerationResult> ModerateAsync(string content, string contentType = "text/plain", CancellationToken cancellationToken = default)
    {
        // Simple mock that flags certain keywords
        var isUnsafe = content.Contains("hate", StringComparison.OrdinalIgnoreCase) ||
                      content.Contains("violence", StringComparison.OrdinalIgnoreCase);

        if (isUnsafe)
        {
            return Task.FromResult(new ModerationResult(
                IsSafe: false,
                SafetyInfo: new Bipins.AI.Core.Models.SafetyInfo(Flagged: true, Categories: new Dictionary<string, bool> { ["hate"] = true }),
                Violations: new[]
                {
                    new SafetyViolation(SafetyCategory.Hate, SafetySeverity.Medium, 0.8, Reason: "Mock violation")
                }));
        }

        return Task.FromResult(new ModerationResult(
            IsSafe: true,
            SafetyInfo: new Bipins.AI.Core.Models.SafetyInfo(Flagged: false),
            Violations: Array.Empty<SafetyViolation>()));
    }

    public Task<bool> IsSafeAsync(string content, string contentType = "text/plain", CancellationToken cancellationToken = default)
    {
        var result = ModerateAsync(content, contentType, cancellationToken);
        return Task.FromResult(result.Result.IsSafe);
    }
}
