using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Bipins.AI.Validation.JsonSchema;

/// <summary>
/// NJsonSchema-based response validator.
/// </summary>
/// <typeparam name="T">The type of response to validate.</typeparam>
public class NJsonSchemaValidator<T> : IResponseValidator<T>
{
    private readonly ILogger<NJsonSchemaValidator<T>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NJsonSchemaValidator{T}"/> class.
    /// </summary>
    public NJsonSchemaValidator(ILogger<NJsonSchemaValidator<T>>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(
        T response, 
        string? schema = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(schema))
        {
            _logger?.LogDebug("No schema provided for validation of {Type}", typeof(T).Name);
            return new ValidationResult(
                IsValid: true,
                Errors: Array.Empty<ValidationError>(),
                Warnings: Array.Empty<ValidationWarning>());
        }

        try
        {
            var jsonSchema = await NJsonSchema.JsonSchema.FromJsonAsync(schema);
            
            // Serialize the response to JSON
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var validationErrors = jsonSchema.Validate(json);

            var errors = validationErrors.Select(e => new ValidationError(
                PropertyName: e.Path ?? "root",
                ErrorMessage: e.Kind.ToString(),
                ErrorCode: e.Kind.ToString(),
                AttemptedValue: null)).ToList();

            var warnings = new List<ValidationWarning>();

            _logger?.LogDebug(
                "JSON Schema validation result for {Type}: Valid={IsValid}, Errors={ErrorCount}",
                typeof(T).Name,
                errors.Count == 0,
                errors.Count);

            return new ValidationResult(
                IsValid: errors.Count == 0,
                Errors: errors,
                Warnings: warnings);
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

