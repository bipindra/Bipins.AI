namespace Bipins.AI.Runtime.Policies;

/// <summary>
/// AI policy for a tenant.
/// </summary>
/// <param name="AllowedProviders">List of allowed provider names.</param>
/// <param name="MaxTokens">Maximum tokens allowed per request.</param>
/// <param name="AllowedTools">List of allowed tool names (null = all allowed).</param>
/// <param name="LoggingFlags">Flags for what to log.</param>
/// <param name="RedactionFlags">Flags for what to redact in logs.</param>
public record AiPolicy(
    IReadOnlyList<string> AllowedProviders,
    int? MaxTokens = null,
    IReadOnlyList<string>? AllowedTools = null,
    LoggingFlags LoggingFlags = LoggingFlags.None,
    RedactionFlags RedactionFlags = RedactionFlags.None);
