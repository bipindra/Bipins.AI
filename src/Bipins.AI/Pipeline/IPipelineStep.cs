namespace Bipins.AI.Runtime.Pipeline;

/// <summary>
/// Contract for a pipeline step.
/// </summary>
/// <typeparam name="TIn">Input type.</typeparam>
/// <typeparam name="TOut">Output type.</typeparam>
public interface IPipelineStep<in TIn, TOut>
{
    /// <summary>
    /// Executes the pipeline step.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="context">The pipeline context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The step result.</returns>
    Task<PipelineStepResult<TOut>> ExecuteAsync(
        TIn input,
        PipelineContext context,
        CancellationToken cancellationToken = default);
}
