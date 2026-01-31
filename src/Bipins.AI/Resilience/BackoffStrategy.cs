namespace Bipins.AI.Resilience;

/// <summary>
/// Backoff strategies for retries.
/// </summary>
public enum BackoffStrategy
{
    Fixed,
    Linear,
    Exponential
}
