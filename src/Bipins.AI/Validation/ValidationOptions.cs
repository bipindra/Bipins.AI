namespace Bipins.AI.Validation;

/// <summary>
/// Options for validation.
/// </summary>
public class ValidationOptions
{
    /// <summary>
    /// Whether validation is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to throw exceptions on validation failures.
    /// </summary>
    public bool ThrowOnValidationFailure { get; set; } = false;

    /// <summary>
    /// Whether to validate requests.
    /// </summary>
    public bool ValidateRequests { get; set; } = true;

    /// <summary>
    /// Whether to validate responses.
    /// </summary>
    public bool ValidateResponses { get; set; } = true;

    /// <summary>
    /// Whether to include warnings in validation results.
    /// </summary>
    public bool IncludeWarnings { get; set; } = true;
}
