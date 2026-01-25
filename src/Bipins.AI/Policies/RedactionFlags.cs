namespace Bipins.AI.Runtime.Policies;

/// <summary>
/// Flags for redaction behavior.
/// </summary>
[Flags]
public enum RedactionFlags
{
    /// <summary>
    /// No redaction.
    /// </summary>
    None = 0,

    /// <summary>
    /// Redact request content.
    /// </summary>
    RedactRequests = 1,

    /// <summary>
    /// Redact response content.
    /// </summary>
    RedactResponses = 2,

    /// <summary>
    /// Redact all.
    /// </summary>
    All = RedactRequests | RedactResponses
}
