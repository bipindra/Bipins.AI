using Bipins.AI.Safety;

namespace Bipins.AI.Guardian.Services;

/// <summary>
/// Mock content moderator for demonstration purposes.
/// In production, use Azure Content Moderator or another service.
/// </summary>
public class MockContentModerator : IContentModerator
{
    private readonly ILogger<MockContentModerator>? _logger;

    public MockContentModerator(ILogger<MockContentModerator>? logger = null)
    {
        _logger = logger;
    }

    public Task<ModerationResult> ModerateAsync(string content, string contentType = "text/plain", CancellationToken cancellationToken = default)
    {
        // Simple keyword-based moderation for demo
        var unsafeKeywords = new Dictionary<SafetyCategory, string[]>
        {
            { SafetyCategory.Hate, new[] { "hate", "discrimination", "prejudice" } },
            { SafetyCategory.Violence, new[] { "violence", "attack", "harm", "kill" } },
            { SafetyCategory.SelfHarm, new[] { "suicide", "self-harm", "hurt myself" } },
            { SafetyCategory.Profanity, new[] { "damn", "hell" } }, // Keeping it simple for demo
            { SafetyCategory.PromptInjection, new[] { "ignore previous", "forget instructions", "system prompt" } }
        };

        var violations = new List<SafetyViolation>();
        var categories = new Dictionary<string, bool>();

        foreach (var (category, keywords) in unsafeKeywords)
        {
            foreach (var keyword in keywords)
            {
                if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    var severity = category == SafetyCategory.SelfHarm || category == SafetyCategory.PromptInjection
                        ? SafetySeverity.High
                        : SafetySeverity.Medium;

                    violations.Add(new SafetyViolation(
                        Category: category,
                        Severity: severity,
                        Confidence: 0.8,
                        Reason: $"Detected {category} content"));

                    categories[category.ToString().ToLowerInvariant()] = true;
                    break;
                }
            }
        }

        var isSafe = violations.Count == 0;

        _logger?.LogDebug(
            "Content moderation: Safe={IsSafe}, Violations={Count}",
            isSafe,
            violations.Count);

        return Task.FromResult(new ModerationResult(
            IsSafe: isSafe,
            SafetyInfo: new Bipins.AI.Core.Models.SafetyInfo(
                Flagged: !isSafe,
                Categories: categories.Count > 0 ? categories : null),
            Violations: violations));
    }

    public Task<bool> IsSafeAsync(string content, string contentType = "text/plain", CancellationToken cancellationToken = default)
    {
        var result = ModerateAsync(content, contentType, cancellationToken);
        return Task.FromResult(result.Result.IsSafe);
    }
}
