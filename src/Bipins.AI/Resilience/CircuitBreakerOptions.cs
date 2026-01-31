namespace Bipins.AI.Resilience;

/// <summary>
/// Options for circuit breaker policy.
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Number of failures before opening the circuit.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Duration the circuit stays open before attempting to close.
    /// </summary>
    public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Number of successful calls needed to close the circuit.
    /// </summary>
    public int SuccessThreshold { get; set; } = 2;
}
