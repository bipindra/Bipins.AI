namespace Bipins.AI.Runtime.Pipeline;

/// <summary>
/// Retry policy for pipeline steps.
/// </summary>
/// <param name="MaxAttempts">Maximum number of retry attempts.</param>
/// <param name="InitialDelay">Initial delay in milliseconds.</param>
/// <param name="MaxDelay">Maximum delay in milliseconds.</param>
/// <param name="BackoffMultiplier">Multiplier for exponential backoff.</param>
public record RetryPolicy(
    int MaxAttempts = 3,
    int InitialDelay = 1000,
    int MaxDelay = 30000,
    double BackoffMultiplier = 2.0);
