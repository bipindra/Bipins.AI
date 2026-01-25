using Microsoft.Extensions.Logging;

namespace Bipins.AI.Runtime.Pipeline;

/// <summary>
/// Handles retry logic for pipeline steps.
/// </summary>
public class StepRetryHandler
{
    private readonly ILogger<StepRetryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StepRetryHandler"/> class.
    /// </summary>
    public StepRetryHandler(ILogger<StepRetryHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes a step with retry logic.
    /// </summary>
    public async Task<PipelineStepResult<TOut>> ExecuteWithRetryAsync<TIn, TOut>(
        IPipelineStep<TIn, TOut> step,
        TIn input,
        PipelineContext context,
        RetryPolicy? retryPolicy = null,
        CancellationToken cancellationToken = default)
    {
        retryPolicy ??= new RetryPolicy();
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < retryPolicy.MaxAttempts)
        {
            try
            {
                var result = await step.ExecuteAsync(input, context, cancellationToken);
                if (result.Success)
                {
                    return result;
                }

                lastException = new InvalidOperationException(result.Error ?? "Step execution failed");
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            attempt++;
            if (attempt < retryPolicy.MaxAttempts)
            {
                var delay = CalculateDelay(attempt, retryPolicy);
                _logger.LogWarning(
                    "Step execution failed (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}ms. Error: {Error}",
                    attempt,
                    retryPolicy.MaxAttempts,
                    delay,
                    lastException?.Message);

                await Task.Delay(delay, cancellationToken);
            }
        }

        _logger.LogError(
            "Step execution failed after {Attempts} attempts. Error: {Error}",
            retryPolicy.MaxAttempts,
            lastException?.Message);

        return new PipelineStepResult<TOut>(
            false,
            default,
            lastException?.Message ?? "Step execution failed after retries",
            0);
    }

    private static int CalculateDelay(int attempt, RetryPolicy policy)
    {
        var delay = (int)(policy.InitialDelay * Math.Pow(policy.BackoffMultiplier, attempt - 1));
        return Math.Min(delay, policy.MaxDelay);
    }
}
