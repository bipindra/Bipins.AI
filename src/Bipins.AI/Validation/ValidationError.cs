namespace Bipins.AI.Validation;

/// <summary>
/// Represents a validation error.
/// </summary>
/// <param name="PropertyName">Name of the property that failed validation.</param>
/// <param name="ErrorMessage">Error message.</param>
/// <param name="ErrorCode">Error code.</param>
/// <param name="AttemptedValue">The value that failed validation.</param>
public record ValidationError(
    string PropertyName,
    string ErrorMessage,
    string? ErrorCode = null,
    object? AttemptedValue = null);
