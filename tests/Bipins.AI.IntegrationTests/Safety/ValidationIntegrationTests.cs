using Bipins.AI.Core.Models;
using Bipins.AI.Providers;
using Bipins.AI.Providers.OpenAI;
using Bipins.AI.Validation;
using Bipins.AI.Validation.JsonSchema;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bipins.AI.IntegrationTests.Safety;

[Collection("Integration")]
public class ValidationIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public ValidationIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Requires OpenAI API key")]
    public async Task ChatResponse_WithJsonSchemaValidation_ValidatesStructure()
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

        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Say 'Hello' and nothing else.")
        });

        var response = await provider.ChatAsync(request);

        // Validate response structure with JSON Schema
        var schema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""content"": { ""type"": ""string"" },
                ""modelId"": { ""type"": ""string"" }
            },
            ""required"": [""content""]
        }";

        var validator = new JsonSchemaValidator(_fixture.Services.GetRequiredService<ILogger<JsonSchemaValidator>>());
        var validationResult = await validator.ValidateAsync(
            System.Text.Json.JsonSerializer.Serialize(response),
            schema);

        Assert.True(validationResult.IsValid, $"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
    }

    [Fact(Skip = "Requires OpenAI API key")]
    public async Task ChatRequest_WithFluentValidation_ValidatesInput()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            return;
        }

        // Create a validator for ChatRequest
        var validator = new ChatRequestValidator();
        var fluentValidator = new Validation.FluentValidation.FluentValidationValidator<ChatRequest>(
            validator,
            _fixture.Services.GetRequiredService<ILogger<Validation.FluentValidation.FluentValidationValidator<ChatRequest>>>());

        var validRequest = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Hello")
        });

        var validationResult = await fluentValidator.ValidateAsync(validRequest);

        Assert.True(validationResult.IsValid);
    }
}

// FluentValidation validator for ChatRequest
public class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    public ChatRequestValidator()
    {
        RuleFor(x => x.Messages)
            .NotEmpty()
            .WithMessage("Messages cannot be empty");

        RuleForEach(x => x.Messages)
            .ChildRules(m =>
            {
                m.RuleFor(msg => msg.Content)
                    .NotEmpty()
                    .When(msg => msg.Role != MessageRole.System)
                    .WithMessage("Message content cannot be empty");
            });

        RuleFor(x => x.Temperature)
            .InclusiveBetween(0f, 2f)
            .When(x => x.Temperature.HasValue)
            .WithMessage("Temperature must be between 0 and 2");

        RuleFor(x => x.MaxTokens)
            .GreaterThan(0)
            .When(x => x.MaxTokens.HasValue)
            .WithMessage("MaxTokens must be greater than 0");
    }
}
