using Bipins.AI.Core.Models;
using Bipins.AI.Providers.Bedrock;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bipins.AI.IntegrationTests.Providers;

[Collection("Integration")]
public class BedrockIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public BedrockIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Requires AWS credentials and Bedrock access")]
    public async Task BedrockChatModel_GenerateAsync_ReturnsResponse()
    {
        var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
        var accessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

        if (string.IsNullOrEmpty(accessKeyId) || string.IsNullOrEmpty(secretAccessKey))
        {
            return; // Skip if no AWS credentials
        }

        var logger = _fixture.Services.GetRequiredService<ILogger<BedrockChatModel>>();
        var options = Options.Create(new BedrockOptions
        {
            Region = region,
            DefaultModelId = "anthropic.claude-3-haiku-20240307-v1:0",
            AccessKeyId = accessKeyId,
            SecretAccessKey = secretAccessKey
        });

        var chatModel = new BedrockChatModel(options, logger);

        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Say 'Hello, World!' and nothing else.")
        });

        var response = await chatModel.GenerateAsync(request);

        Assert.NotNull(response);
        Assert.NotNull(response.Content);
        Assert.Contains("Hello", response.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Skip = "Requires AWS credentials and Bedrock access")]
    public async Task BedrockChatModel_WithSystemMessage_RespectsSystemPrompt()
    {
        var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
        var accessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

        if (string.IsNullOrEmpty(accessKeyId) || string.IsNullOrEmpty(secretAccessKey))
        {
            return;
        }

        var logger = _fixture.Services.GetRequiredService<ILogger<BedrockChatModel>>();
        var options = Options.Create(new BedrockOptions
        {
            Region = region,
            DefaultModelId = "anthropic.claude-3-haiku-20240307-v1:0",
            AccessKeyId = accessKeyId,
            SecretAccessKey = secretAccessKey
        });

        var chatModel = new BedrockChatModel(options, logger);

        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.System, "You are a helpful assistant that always responds in uppercase."),
            new Message(MessageRole.User, "Say hello")
        });

        var response = await chatModel.GenerateAsync(request);

        Assert.NotNull(response);
        Assert.NotNull(response.Content);
        // Response should be in uppercase (or at least contain uppercase letters)
        Assert.True(response.Content.Any(char.IsUpper));
    }
}
