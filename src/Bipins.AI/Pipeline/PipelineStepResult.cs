namespace Bipins.AI.Runtime.Pipeline;

/// <summary>
/// Result of a pipeline step execution.
/// </summary>
/// <typeparam name="T">The result type.</typeparam>
/// <param name="Success">Whether the step succeeded.</param>
/// <param name="Value">The result value (if successful).</param>
/// <param name="Error">Error message (if failed).</param>
/// <param name="Duration">Duration in milliseconds.</param>
public record PipelineStepResult<T>(
    bool Success,
    T? Value = default,
    string? Error = null,
    long Duration = 0);
