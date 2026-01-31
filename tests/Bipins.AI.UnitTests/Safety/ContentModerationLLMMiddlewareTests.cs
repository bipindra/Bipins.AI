using Bipins.AI.Core.Models;
using Bipins.AI.Safety;
using Bipins.AI.Safety.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Safety;

public class ContentModerationLLMMiddlewareTests
{
    private readonly Mock<IContentModerator> _mockModerator;
    private readonly ContentModerationOptions _options;
    private readonly Mock<ILogger<ContentModerationLLMMiddleware>> _mockLogger;
    private readonly ContentModerationLLMMiddleware _middleware;

    public ContentModerationLLMMiddlewareTests()
    {
        _mockModerator = new Mock<IContentModerator>();
        _options = new ContentModerationOptions
        {
            Enabled = true,
            MinimumSeverityToBlock = SafetySeverity.Medium,
            BlockedCategories = new List<SafetyCategory> { SafetyCategory.PromptInjection }
        };
        _mockLogger = new Mock<ILogger<ContentModerationLLMMiddleware>>();
        _middleware = new ContentModerationLLMMiddleware(
            _mockModerator.Object,
            Options.Create(_options),
            _mockLogger.Object);
    }

    [Fact]
    public async Task OnRequestAsync_WhenContentIsSafe_ReturnsOriginalRequest()
    {
        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Hello, how are you?")
        });

        _mockModerator.Setup(m => m.ModerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModerationResult(
                IsSafe: true,
                SafetyInfo: new Bipins.AI.Core.Models.SafetyInfo(Flagged: false),
                Violations: Array.Empty<SafetyViolation>()));

        var result = await _middleware.OnRequestAsync(request);

        Assert.Equal(request, result);
    }

    [Fact]
    public async Task OnRequestAsync_WhenContentIsUnsafe_ThrowsException()
    {
        var request = new ChatRequest(new[]
        {
            new Message(MessageRole.User, "Unsafe content")
        });

        _mockModerator.Setup(m => m.ModerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModerationResult(
                IsSafe: false,
                SafetyInfo: new Bipins.AI.Core.Models.SafetyInfo(Flagged: true, Categories: new Dictionary<string, bool> { ["hate"] = true }),
                Violations: new[]
                {
                    new SafetyViolation(SafetyCategory.PromptInjection, SafetySeverity.High, 0.9, Reason: "Prompt injection detected")
                }));

        _options.ThrowOnUnsafeContent = true;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _middleware.OnRequestAsync(request));
    }

    [Fact]
    public async Task OnResponseAsync_WhenResponseIsSafe_ReturnsOriginalResponse()
    {
        var request = new ChatRequest(new[] { new Message(MessageRole.User, "Hello") });
        var response = new ChatResponse("Hello! How can I help you?");

        _mockModerator.Setup(m => m.ModerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModerationResult(
                IsSafe: true,
                SafetyInfo: new Bipins.AI.Core.Models.SafetyInfo(Flagged: false),
                Violations: Array.Empty<SafetyViolation>()));

        var result = await _middleware.OnResponseAsync(request, response);

        Assert.Equal(response.Content, result.Content);
        Assert.NotNull(result.Safety);
    }

    [Fact]
    public async Task OnResponseAsync_WhenResponseIsUnsafeAndFilteringEnabled_FiltersContent()
    {
        var request = new ChatRequest(new[] { new Message(MessageRole.User, "Hello") });
        var response = new ChatResponse("Unsafe response content");

        _mockModerator.Setup(m => m.ModerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModerationResult(
                IsSafe: false,
                SafetyInfo: new Bipins.AI.Core.Models.SafetyInfo(Flagged: true),
                Violations: new[]
                {
                    new SafetyViolation(SafetyCategory.Hate, SafetySeverity.High, 0.9)
                }));

        _options.FilterUnsafeContent = true;
        _options.ReplacementText = "[Filtered]";

        var result = await _middleware.OnResponseAsync(request, response);

        Assert.Equal("[Filtered]", result.Content);
        Assert.True(result.Safety?.Flagged ?? false);
    }

    [Fact]
    public async Task OnEmbeddingRequestAsync_WhenContentIsSafe_ReturnsOriginalText()
    {
        _mockModerator.Setup(m => m.ModerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModerationResult(
                IsSafe: true,
                SafetyInfo: new Bipins.AI.Core.Models.SafetyInfo(Flagged: false),
                Violations: Array.Empty<SafetyViolation>()));

        var result = await _middleware.OnEmbeddingRequestAsync("Safe text");

        Assert.Equal("Safe text", result);
    }
}
