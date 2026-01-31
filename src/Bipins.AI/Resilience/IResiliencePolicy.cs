namespace Bipins.AI.Resilience;

/// <summary>
/// Interface for resilience policies.
/// </summary>
public interface IResiliencePolicy
{
    /// <summary>
    /// Executes an action with the resilience policy.
    /// </summary>
    Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a function with the resilience policy.
    /// </summary>
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
}
