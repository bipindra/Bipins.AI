namespace Bipins.AI.Safety;

/// <summary>
/// Represents a safety violation found in content.
/// </summary>
/// <param name="Category">Category of the violation.</param>
/// <param name="Severity">Severity level of the violation.</param>
/// <param name="Confidence">Confidence score (0.0 - 1.0).</param>
/// <param name="StartIndex">Start index of the violation in the content (if applicable).</param>
/// <param name="EndIndex">End index of the violation in the content (if applicable).</param>
/// <param name="Reason">Reason or description of the violation.</param>
public record SafetyViolation(
    SafetyCategory Category,
    SafetySeverity Severity,
    double Confidence,
    int? StartIndex = null,
    int? EndIndex = null,
    string? Reason = null);
