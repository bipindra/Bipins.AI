using Bipins.AI.Core.Models;
using Bipins.AI.Providers;
using Bipins.AI.Providers.OpenAI;
using Bipins.AI.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bipins.AI.IntegrationTests.Safety;

[Collection("Integration")]
public class ResilienceIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public ResilienceIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Requires OpenAI API key")]
    public async Task ResiliencePolicy_WithRetry_HandlesTransientFailures()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            return;
        }

        var options = new ResilienceOptions
        {
            Retry = new RetryOptions
            {
                MaxRetries = 2,
                Delay = TimeSpan.FromMilliseconds(100),
                BackoffStrategy = BackoffStrategy.Exponential
            },
            Timeout = new TimeoutOptions
            {
                Timeout = TimeSpan.FromSeconds(30)
            }
        };

        var logger = _fixture.Services.GetRequiredService<ILogger<PollyResiliencePolicy>>();
        var policy = new PollyResiliencePolicy(options, logger);

        // Test that policy can execute successfully
        var result = await policy.ExecuteAsync(async () =>
        {
            await Task.Delay(10);
            return 42;
        });

        Assert.Equal(42, result);
    }

    [Fact(Skip = "Requires OpenAI API key")]
    public async Task OpenAIProvider_WithResiliencePolicy_HandlesNetworkIssues()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            return;
        }

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

        var provider = new OpenAiLLMProvider(chatModel, chatModelStreaming, embeddingModel, openAiOptions, logger);

        var resilienceOptions = new ResilienceOptions
        {
            Retry = new RetryOptions
            {
                MaxRetries = 1,
                Delay = TimeSpan.FromMilliseconds(100)
            }
        };

        var resilienceLogger = _fixture.Services.GetRequiredService<ILogger<PollyResiliencePolicy>>();
        var resiliencePolicy = new PollyResiliencePolicy(resilienceOptions, resilienceLogger);

        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Say 'Hello' and nothing else.")
        });

        // Execute with resilience policy
        var response = await resiliencePolicy.ExecuteAsync(async () =>
        {
            return await provider.ChatAsync(request);
        });

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Content));
    }
}
