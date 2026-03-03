using Bipins.AI.Core.Models;
using Bipins.AI.Providers.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Xunit;

namespace Bipins.AI.IntegrationTests.Providers;

[Collection("Integration")]
public class OpenAiIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly string? _apiKey;
    private readonly string _baseUrl;
    private readonly string _chatModelId;
    private readonly string _embeddingModelId;

    public OpenAiIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com/v1";
        _chatModelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";
        _embeddingModelId = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL") ?? "text-embedding-3-small";
    }

    private bool ShouldSkipTest()
    {
        return string.IsNullOrEmpty(_apiKey);
    }

    private IOptions<OpenAiOptions> CreateOptions()
    {
        return Options.Create(new OpenAiOptions
        {
            ApiKey = _apiKey!,
            BaseUrl = _baseUrl,
            DefaultChatModelId = _chatModelId,
            DefaultEmbeddingModelId = _embeddingModelId
        });
    }

    private OpenAiChatModel CreateChatModel()
    {
        var httpClientFactory = _fixture.Services.GetRequiredService<IHttpClientFactory>();
        var logger = _fixture.Services.GetRequiredService<ILogger<OpenAiChatModel>>();
        var options = CreateOptions();
        return new OpenAiChatModel(httpClientFactory, options, logger);
    }

    private OpenAiChatModelStreaming CreateStreamingModel()
    {
        var httpClientFactory = _fixture.Services.GetRequiredService<IHttpClientFactory>();
        var logger = _fixture.Services.GetRequiredService<ILogger<OpenAiChatModelStreaming>>();
        var options = CreateOptions();
        return new OpenAiChatModelStreaming(httpClientFactory, options, logger);
    }

    private OpenAiEmbeddingModel CreateEmbeddingModel()
    {
        var httpClientFactory = _fixture.Services.GetRequiredService<IHttpClientFactory>();
        var logger = _fixture.Services.GetRequiredService<ILogger<OpenAiEmbeddingModel>>();
        var options = CreateOptions();
        return new OpenAiEmbeddingModel(httpClientFactory, options, logger);
    }

    private OpenAiLLMProvider CreateProvider()
    {
        var httpClientFactory = _fixture.Services.GetRequiredService<IHttpClientFactory>();
        var chatLogger = _fixture.Services.GetRequiredService<ILogger<OpenAiChatModel>>();
        var streamingLogger = _fixture.Services.GetRequiredService<ILogger<OpenAiChatModelStreaming>>();
        var embeddingLogger = _fixture.Services.GetRequiredService<ILogger<OpenAiEmbeddingModel>>();
        var providerLogger = _fixture.Services.GetRequiredService<ILogger<OpenAiLLMProvider>>();
        var options = CreateOptions();

        var chatModel = new OpenAiChatModel(httpClientFactory, options, chatLogger);
        var streamingModel = new OpenAiChatModelStreaming(httpClientFactory, options, streamingLogger);
        var embeddingModel = new OpenAiEmbeddingModel(httpClientFactory, options, embeddingLogger);

        return new OpenAiLLMProvider(chatModel, streamingModel, embeddingModel, options, providerLogger);
    }

    [Fact]
    public async Task OpenAiChatModel_GenerateAsync_ReturnsResponse()
    {
        if (ShouldSkipTest())
        {
            return; // Skip if no API key
        }

        var chatModel = CreateChatModel();

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
        if (ShouldSkipTest())
        {
            return; // Skip if no API key
        }

        var embeddingModel = CreateEmbeddingModel();

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
        if (ShouldSkipTest())
        {
            return; // Skip if no API key
        }

        var streamingModel = CreateStreamingModel();

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
        if (ShouldSkipTest())
        {
            return; // Skip if no API key
        }

        var provider = CreateProvider();

        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Say 'Hello from provider' and nothing else.")
        });

        var response = await provider.ChatAsync(request);

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Content));
        Assert.Contains("Hello", response.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenAiLLMProvider_ChatAsync_WithStructuredOutput_ReturnsJsonPlan()
    {
        // This test demonstrates how ChatAsync is used in LLMPlanner with structured output
        if (ShouldSkipTest())
        {
            return; // Skip if no API key
        }

        var provider = CreateProvider();

        // Simulate the planning request similar to LLMPlanner
        var goal = "Calculate the sum of 15 and 23, then multiply the result by 2";
        var planningPrompt = $@"Goal: {goal}

Available tools:
- calculator: Performs basic arithmetic operations (add, subtract, multiply, divide)

Create a detailed step-by-step plan to accomplish the goal. Include which tools to use and what parameters they need.";

        var messages = new List<Message>
        {
            new(MessageRole.System, "You are a planning assistant. Create a step-by-step plan to accomplish the given goal. Return the plan as JSON with this structure: {\"steps\": [{\"order\": 1, \"action\": \"...\", \"toolName\": \"...\", \"parameters\": {...}, \"expectedOutcome\": \"...\"}], \"reasoning\": \"...\"}"),
            new(MessageRole.User, planningPrompt)
        };

        // Define structured output schema (OpenAI strict mode: additionalProperties false, required must list every property)
        var structuredOutputSchema = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                steps = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            order = new { type = "integer" },
                            action = new { type = "string" }
                        },
                        required = new[] { "order", "action" },
                        additionalProperties = false
                    }
                },
                reasoning = new { type = "string" }
            },
            required = new[] { "steps", "reasoning" },
            additionalProperties = false
        });

        var request = new ChatRequest(
            Messages: messages,
            Temperature: 0.3f,
            MaxTokens: 2000,
            StructuredOutput: new StructuredOutputOptions(
                Schema: structuredOutputSchema,
                ResponseFormat: "json_schema"));

        var response = await provider.ChatAsync(request);

        // Verify response
        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Content));

        // Verify structured output
        Assert.True(response.StructuredOutput.HasValue, "Expected structured output to be present");
        var structuredOutput = response.StructuredOutput!.Value;

        // Parse and validate the plan structure
        Assert.True(structuredOutput.TryGetProperty("steps", out var stepsElement), "Expected 'steps' property in structured output");
        Assert.Equal(JsonValueKind.Array, stepsElement.ValueKind);

        var steps = stepsElement.EnumerateArray().ToList();
        Assert.NotEmpty(steps);

        // Validate first step structure
        var firstStep = steps[0];
        Assert.True(firstStep.TryGetProperty("order", out var orderProp), "Expected 'order' property in step");
        Assert.True(firstStep.TryGetProperty("action", out var actionProp), "Expected 'action' property in step");
        Assert.False(string.IsNullOrWhiteSpace(actionProp.GetString()));

        // Verify the plan makes sense for the goal
        var allStepsText = string.Join(" ", steps.Select(s => 
            s.TryGetProperty("action", out var a) ? a.GetString() : ""));
        Assert.Contains("15", allStepsText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("23", allStepsText, StringComparison.OrdinalIgnoreCase);

        // If reasoning is present, it should not be empty
        if (structuredOutput.TryGetProperty("reasoning", out var reasoningProp))
        {
            var reasoning = reasoningProp.GetString();
            if (!string.IsNullOrWhiteSpace(reasoning))
            {
                Assert.NotEmpty(reasoning);
            }
        }
    }
}

