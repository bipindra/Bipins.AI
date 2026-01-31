using Bipins.AI.Core.Models;

namespace Bipins.AI.Safety;

/// <summary>
/// Result of content moderation.
/// </summary>
/// <param name="IsSafe">Whether the content is safe.</param>
/// <param name="SafetyInfo">Safety information from the moderation service.</param>
/// <param name="Violations">List of safety violations found.</param>
/// <param name="FilteredContent">Filtered version of the content (if filtering was applied).</param>
/// <param name="Confidence">Confidence score of the moderation decision (0.0 - 1.0).</param>
public record ModerationResult(
    bool IsSafe,
    SafetyInfo SafetyInfo,
    IReadOnlyList<SafetyViolation> Violations,
    string? FilteredContent = null,
    double Confidence = 1.0);
