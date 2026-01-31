using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Validation.FluentValidation;

/// <summary>
/// FluentValidation-based request validator.
/// </summary>
/// <typeparam name="T">The type to validate.</typeparam>
public class FluentValidationValidator<T> : IRequestValidator<T>
{
    private readonly IValidator<T> _validator;
    private readonly ILogger<FluentValidationValidator<T>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentValidationValidator{T}"/> class.
    /// </summary>
    public FluentValidationValidator(IValidator<T> validator, ILogger<FluentValidationValidator<T>>? logger = null)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(T request, CancellationToken cancellationToken = default)
    {
        var fluentResult = await _validator.ValidateAsync(request, cancellationToken);

        var errors = fluentResult.Errors.Select(e => new ValidationError(
            PropertyName: e.PropertyName,
            ErrorMessage: e.ErrorMessage,
            ErrorCode: e.ErrorCode,
            AttemptedValue: e.AttemptedValue)).ToList();

        var warnings = new List<ValidationWarning>();

        _logger?.LogDebug(
            "Validation result for {Type}: Valid={IsValid}, Errors={ErrorCount}",
            typeof(T).Name,
            fluentResult.IsValid,
            errors.Count);

        return new ValidationResult(
            IsValid: fluentResult.IsValid,
            Errors: errors,
            Warnings: warnings);
    }
}
