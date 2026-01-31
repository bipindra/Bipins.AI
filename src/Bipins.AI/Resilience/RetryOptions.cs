namespace Bipins.AI.Resilience;

/// <summary>
/// Options for retry policy.
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// Maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay between retries.
    /// </summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Backoff strategy for retries.
    /// </summary>
    public BackoffStrategy BackoffStrategy { get; set; } = BackoffStrategy.Exponential;

    /// <summary>
    /// Maximum delay between retries.
    /// </summary>
    public TimeSpan? MaxDelay { get; set; }

    /// <summary>
    /// Types of exceptions to retry on.
    /// </summary>
    public IReadOnlyList<Type>? RetryableExceptions { get; set; }
}
