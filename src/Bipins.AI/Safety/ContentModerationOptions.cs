namespace Bipins.AI.Safety;

/// <summary>
/// Options for content moderation.
/// </summary>
public class ContentModerationOptions
{
    /// <summary>
    /// Whether content moderation is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to filter unsafe content automatically.
    /// </summary>
    public bool FilterUnsafeContent { get; set; } = false;

    /// <summary>
    /// Replacement text for filtered content.
    /// </summary>
    public string? ReplacementText { get; set; } = "[Content filtered]";

    /// <summary>
    /// Minimum severity level to block content.
    /// </summary>
    public SafetySeverity MinimumSeverityToBlock { get; set; } = SafetySeverity.Medium;

    /// <summary>
    /// Categories to block regardless of severity.
    /// </summary>
    public IReadOnlyList<SafetyCategory> BlockedCategories { get; set; } = new List<SafetyCategory>
    {
        SafetyCategory.PromptInjection,
        SafetyCategory.SelfHarm
    };

    /// <summary>
    /// Whether to throw exceptions for unsafe content.
    /// </summary>
    public bool ThrowOnUnsafeContent { get; set; } = false;

    /// <summary>
    /// Minimum confidence score to consider a violation (0.0 - 1.0).
    /// </summary>
    public double MinimumConfidence { get; set; } = 0.5;
}
