namespace Bipins.AI.Resilience;

/// <summary>
/// Options for configuring resilience policies.
/// </summary>
public class ResilienceOptions
{
    /// <summary>
    /// Retry policy options.
    /// </summary>
    public RetryOptions? Retry { get; set; }

    /// <summary>
    /// Circuit breaker options.
    /// </summary>
    public CircuitBreakerOptions? CircuitBreaker { get; set; }

    /// <summary>
    /// Timeout options.
    /// </summary>
    public TimeoutOptions? Timeout { get; set; }

    /// <summary>
    /// Bulkhead options (concurrency limiting).
    /// </summary>
    public BulkheadOptions? Bulkhead { get; set; }
}
