namespace Bipins.AI.Resilience;

/// <summary>
/// Options for bulkhead policy (concurrency limiting).
/// </summary>
public class BulkheadOptions
{
    /// <summary>
    /// Maximum number of concurrent executions.
    /// </summary>
    public int MaxParallelization { get; set; } = 10;

    /// <summary>
    /// Maximum number of queued actions.
    /// </summary>
    public int MaxQueuingActions { get; set; } = 5;
}
