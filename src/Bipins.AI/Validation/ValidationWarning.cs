namespace Bipins.AI.Validation;

/// <summary>
/// Represents a validation warning.
/// </summary>
/// <param name="PropertyName">Name of the property with the warning.</param>
/// <param name="WarningMessage">Warning message.</param>
/// <param name="WarningCode">Warning code.</param>
public record ValidationWarning(
    string PropertyName,
    string WarningMessage,
    string? WarningCode = null);
