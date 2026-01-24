namespace Bipins.AI.Core.Runtime.Policies;

/// <summary>
/// Interface for rate limiting operations.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Attempts to acquire a rate limit token.
    /// </summary>
    /// <param name="key">The rate limit key (e.g., tenant ID, endpoint, model).</param>
    /// <param name="limit">Maximum number of requests allowed.</param>
    /// <param name="window">Time window for the limit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the request is allowed, false if rate limited.</returns>
    Task<bool> TryAcquireAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of seconds to wait before retrying (if rate limited).
    /// </summary>
    /// <param name="key">The rate limit key.</param>
    /// <param name="limit">Maximum number of requests allowed.</param>
    /// <param name="window">Time window for the limit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of seconds to wait, or null if not rate limited.</returns>
    Task<int?> GetRetryAfterAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the rate limit for a key.
    /// </summary>
    /// <param name="key">The rate limit key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task ResetAsync(string key, CancellationToken cancellationToken = default);
}
