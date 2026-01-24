using Bipins.AI.Core.Models;
using Bipins.AI.Providers.AzureOpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bipins.AI.IntegrationTests.Providers;

[Collection("Integration")]
public class AzureOpenAiIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public AzureOpenAiIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Requires Azure OpenAI API key")]
    public async Task AzureOpenAiChatModel_GenerateAsync_ReturnsResponse()
    {
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-35-turbo";

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpoint))
        {
            return; // Skip if no API key
        }

        var httpClientFactory = _fixture.Services.GetRequiredService<IHttpClientFactory>();
        var logger = _fixture.Services.GetRequiredService<ILogger<AzureOpenAiChatModel>>();
        var options = Options.Create(new AzureOpenAiOptions
        {
            ApiKey = apiKey,
            Endpoint = endpoint,
            DefaultChatDeploymentName = deploymentName
        });

        var chatModel = new AzureOpenAiChatModel(httpClientFactory, options, logger);

        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Say 'Hello, World!' and nothing else.")
        });

        var response = await chatModel.GenerateAsync(request);

        Assert.NotNull(response);
        Assert.NotNull(response.Content);
        Assert.Contains("Hello", response.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Skip = "Requires Azure OpenAI API key")]
    public async Task AzureOpenAiChatModel_WithStructuredOutput_ReturnsStructuredResponse()
    {
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-35-turbo";

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpoint))
        {
            return;
        }

        var httpClientFactory = _fixture.Services.GetRequiredService<IHttpClientFactory>();
        var logger = _fixture.Services.GetRequiredService<ILogger<AzureOpenAiChatModel>>();
        var options = Options.Create(new AzureOpenAiOptions
        {
            ApiKey = apiKey,
            Endpoint = endpoint,
            DefaultChatDeploymentName = deploymentName
        });

        var chatModel = new AzureOpenAiChatModel(httpClientFactory, options, logger);

        var schema = System.Text.Json.JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                name = new { type = "string" },
                age = new { type = "number" }
            },
            required = new[] { "name", "age" }
        });

        var structuredOutput = new StructuredOutputOptions(schema, "json_schema");
        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Return a JSON object with name='Test' and age=25")
        }, StructuredOutput: structuredOutput);

        var response = await chatModel.GenerateAsync(request);

        Assert.NotNull(response);
        if (response.StructuredOutput.HasValue)
        {
            Assert.True(response.StructuredOutput.Value.TryGetProperty("name", out _));
            Assert.True(response.StructuredOutput.Value.TryGetProperty("age", out _));
        }
    }
}
