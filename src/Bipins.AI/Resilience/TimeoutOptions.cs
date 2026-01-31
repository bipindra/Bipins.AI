namespace Bipins.AI.Resilience;

/// <summary>
/// Options for timeout policy.
/// </summary>
public class TimeoutOptions
{
    /// <summary>
    /// Timeout duration.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
