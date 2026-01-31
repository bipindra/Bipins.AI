namespace Bipins.AI.Validation;

/// <summary>
/// Interface for validating requests.
/// </summary>
/// <typeparam name="T">The type of request to validate.</typeparam>
public interface IRequestValidator<in T>
{
    /// <summary>
    /// Validates the request.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<ValidationResult> ValidateAsync(T request, CancellationToken cancellationToken = default);
}
