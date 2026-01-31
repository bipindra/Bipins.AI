namespace Bipins.AI.Validation;

/// <summary>
/// Interface for validating responses.
/// </summary>
/// <typeparam name="T">The type of response to validate.</typeparam>
public interface IResponseValidator<in T>
{
    /// <summary>
    /// Validates the response.
    /// </summary>
    /// <param name="response">The response to validate.</param>
    /// <param name="schema">Optional JSON schema for validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<ValidationResult> ValidateAsync(
        T response, 
        string? schema = null, 
        CancellationToken cancellationToken = default);
}
