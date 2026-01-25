namespace Bipins.AI.Core.Models;

/// <summary>
/// Safety information about the response.
/// </summary>
/// <param name="Flagged">Whether the content was flagged.</param>
/// <param name="Categories">Categories of safety concerns.</param>
public record SafetyInfo(
    bool Flagged,
    Dictionary<string, bool>? Categories = null);
