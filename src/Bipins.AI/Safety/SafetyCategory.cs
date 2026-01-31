namespace Bipins.AI.Safety;

/// <summary>
/// Categories of safety violations.
/// </summary>
public enum SafetyCategory
{
    Hate,
    Harassment,
    Violence,
    SelfHarm,
    Sexual,
    Spam,
    Profanity,
    PII, // Personally Identifiable Information
    PromptInjection,
    Other
}
