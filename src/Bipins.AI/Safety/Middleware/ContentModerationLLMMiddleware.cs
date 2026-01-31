using Bipins.AI.Core.Models;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Safety.Middleware;

/// <summary>
/// Content moderation middleware for LLM provider calls.
/// </summary>
public class ContentModerationLLMMiddleware : ILLMProviderMiddleware
{
    private readonly IContentModerator _moderator;
    private readonly ContentModerationOptions _options;
    private readonly ILogger<ContentModerationLLMMiddleware>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentModerationLLMMiddleware"/> class.
    /// </summary>
    public ContentModerationLLMMiddleware(
        IContentModerator moderator,
        Microsoft.Extensions.Options.IOptions<ContentModerationOptions> options,
        ILogger<ContentModerationLLMMiddleware>? logger = null)
    {
        _moderator = moderator ?? throw new ArgumentNullException(nameof(moderator));
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ChatRequest> OnRequestAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return request;
        }

        // Extract text from all messages
        var allText = string.Join(" ", request.Messages
            .Where(m => m.Content != null)
            .Select(m => m.Content));

        if (string.IsNullOrWhiteSpace(allText))
        {
            return request;
        }

        var moderationResult = await _moderator.ModerateAsync(allText, "text/plain", cancellationToken);

        if (!moderationResult.IsSafe)
        {
            _logger?.LogWarning(
                "Unsafe content detected in request: {ViolationCount} violations, Categories: {Categories}",
                moderationResult.Violations.Count,
                string.Join(", ", moderationResult.Violations.Select(v => v.Category)));

            // Check if we should block
            var shouldBlock = moderationResult.Violations.Any(v =>
                _options.BlockedCategories.Contains(v.Category) ||
                v.Severity >= _options.MinimumSeverityToBlock);

            if (shouldBlock)
            {
                if (_options.ThrowOnUnsafeContent)
                {
                    throw new UnauthorizedAccessException(
                        $"Content moderation failed: {string.Join(", ", moderationResult.Violations.Select(v => v.Category))}");
                }

                // Return a safe response instead
                throw new InvalidOperationException(
                    $"Content moderation blocked request due to: {string.Join(", ", moderationResult.Violations.Select(v => v.Category))}");
            }
        }

        return request;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> OnResponseAsync(ChatRequest request, ChatResponse response, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(response.Content))
        {
            return response;
        }

        var moderationResult = await _moderator.ModerateAsync(response.Content, "text/plain", cancellationToken);

        // Merge safety info into response
        var safetyInfo = moderationResult.SafetyInfo;
        if (response.Safety != null)
        {
            // Merge existing safety info
            var mergedCategories = new Dictionary<string, bool>(response.Safety.Categories ?? new Dictionary<string, bool>());
            foreach (var category in safetyInfo.Categories ?? new Dictionary<string, bool>())
            {
                mergedCategories[category.Key] = category.Value;
            }
            safetyInfo = new Core.Models.SafetyInfo(
                Flagged: response.Safety.Flagged || safetyInfo.Flagged,
                Categories: mergedCategories);
        }

        if (!moderationResult.IsSafe)
        {
            _logger?.LogWarning(
                "Unsafe content detected in response: {ViolationCount} violations, Categories: {Categories}",
                moderationResult.Violations.Count,
                string.Join(", ", moderationResult.Violations.Select(v => v.Category)));

            // Check if we should filter
            var shouldFilter = moderationResult.Violations.Any(v =>
                _options.BlockedCategories.Contains(v.Category) ||
                v.Severity >= _options.MinimumSeverityToBlock);

            if (shouldFilter && _options.FilterUnsafeContent)
            {
                var filteredContent = _options.ReplacementText ?? "[Content filtered]";
                return response with
                {
                    Content = filteredContent,
                    Safety = safetyInfo
                };
            }
        }

        return response with { Safety = safetyInfo };
    }

    /// <inheritdoc />
    public async Task<string> OnEmbeddingRequestAsync(string text, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var moderationResult = await _moderator.ModerateAsync(text, "text/plain", cancellationToken);

        if (!moderationResult.IsSafe)
        {
            _logger?.LogWarning(
                "Unsafe content detected in embedding request: {ViolationCount} violations",
                moderationResult.Violations.Count);

            var shouldBlock = moderationResult.Violations.Any(v =>
                _options.BlockedCategories.Contains(v.Category) ||
                v.Severity >= _options.MinimumSeverityToBlock);

            if (shouldBlock)
            {
                if (_options.ThrowOnUnsafeContent)
                {
                    throw new UnauthorizedAccessException(
                        $"Content moderation failed: {string.Join(", ", moderationResult.Violations.Select(v => v.Category))}");
                }

                throw new InvalidOperationException(
                    $"Content moderation blocked embedding request due to: {string.Join(", ", moderationResult.Violations.Select(v => v.Category))}");
            }
        }

        return text;
    }
}
