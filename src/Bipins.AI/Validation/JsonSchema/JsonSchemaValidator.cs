using Microsoft.Extensions.Logging;

namespace Bipins.AI.Validation.JsonSchema;

/// <summary>
/// Generic JSON Schema validator that works with any JSON string.
/// </summary>
public class JsonSchemaValidator : IResponseValidator<string>
{
    private readonly ILogger<JsonSchemaValidator>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchemaValidator"/> class.
    /// </summary>
    public JsonSchemaValidator(ILogger<JsonSchemaValidator>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(
        string response, 
        string? schema = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(schema))
        {
            return new ValidationResult(
                IsValid: true,
                Errors: Array.Empty<ValidationError>(),
                Warnings: Array.Empty<ValidationWarning>());
        }

        if (string.IsNullOrWhiteSpace(response))
        {
            return new ValidationResult(
                IsValid: false,
                Errors: new[]
                {
                    new ValidationError(
                        PropertyName: "response",
                        ErrorMessage: "Response is null or empty",
                        ErrorCode: "EMPTY_RESPONSE")
                },
                Warnings: Array.Empty<ValidationWarning>());
        }

        try
        {
            var jsonSchema = await NJsonSchema.JsonSchema.FromJsonAsync(schema);
            var validationErrors = jsonSchema.Validate(response);

            var errors = validationErrors.Select(e => new ValidationError(
                PropertyName: e.Path ?? "root",
                ErrorMessage: e.Kind.ToString(),
                ErrorCode: e.Kind.ToString(),
                AttemptedValue: null)).ToList();

            _logger?.LogDebug(
                "JSON Schema validation: Valid={IsValid}, Errors={ErrorCount}",
                errors.Count == 0,
                errors.Count);

            return new ValidationResult(
                IsValid: errors.Count == 0,
                Errors: errors,
                Warnings: Array.Empty<ValidationWarning>());
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during JSON Schema validation");
            
            return new ValidationResult(
                IsValid: false,
                Errors: new[]
                {
                    new ValidationError(
                        PropertyName: "schema",
                        ErrorMessage: $"Schema validation failed: {ex.Message}",
                        ErrorCode: "SCHEMA_VALIDATION_ERROR")
                },
                Warnings: Array.Empty<ValidationWarning>());
        }
    }
}
