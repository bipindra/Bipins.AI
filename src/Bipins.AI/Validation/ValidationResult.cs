namespace Bipins.AI.Validation;

/// <summary>
/// Result of validation.
/// </summary>
/// <param name="IsValid">Whether the validation passed.</param>
/// <param name="Errors">List of validation errors.</param>
/// <param name="Warnings">List of validation warnings.</param>
public record ValidationResult(
    bool IsValid,
    IReadOnlyList<ValidationError> Errors,
    IReadOnlyList<ValidationWarning> Warnings);
