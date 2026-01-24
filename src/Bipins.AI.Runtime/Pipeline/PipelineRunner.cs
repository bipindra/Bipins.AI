using Microsoft.Extensions.Logging;

namespace Bipins.AI.Runtime.Pipeline;

/// <summary>
/// Executes a pipeline of steps.
/// </summary>
public class PipelineRunner
{
    private readonly ILogger<PipelineRunner> _logger;
    private readonly StepRetryHandler _retryHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineRunner"/> class.
    /// </summary>
    public PipelineRunner(ILogger<PipelineRunner> logger, StepRetryHandler retryHandler)
    {
        _logger = logger;
        _retryHandler = retryHandler;
    }

    /// <summary>
    /// Executes a pipeline of steps sequentially.
    /// </summary>
    public async Task<PipelineStepResult<TOut>> ExecuteAsync<TIn, TOut>(
        TIn input,
        PipelineContext context,
        IReadOnlyList<IPipelineStep<TIn, TOut>> steps,
        RetryPolicy? retryPolicy = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (steps.Count == 0)
        {
            return new PipelineStepResult<TOut>(false, default, "No steps provided");
        }

        using var cts = timeout.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (timeout.HasValue)
        {
            cts.CancelAfter(timeout.Value);
        }

        var currentInput = input;
        object? currentValue = null;

        foreach (var step in steps)
        {
            var result = await _retryHandler.ExecuteWithRetryAsync(
                step,
                currentInput,
                context,
                retryPolicy,
                cts.Token);

            if (!result.Success)
            {
                _logger.LogError("Pipeline step failed: {Error}", result.Error);
                return new PipelineStepResult<TOut>(false, default, result.Error, result.Duration);
            }

            currentValue = result.Value;
            if (currentValue is TOut output)
            {
                currentInput = (TIn)(object)output;
            }
        }

        if (currentValue is TOut finalOutput)
        {
            return new PipelineStepResult<TOut>(true, finalOutput, null, context.Stopwatch.ElapsedMilliseconds);
        }

        return new PipelineStepResult<TOut>(false, default, "Pipeline did not produce expected output");
    }
}
