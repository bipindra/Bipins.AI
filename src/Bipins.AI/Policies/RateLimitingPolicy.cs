using Bipins.AI.Core.Runtime.Policies;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Runtime.Policies;

/// <summary>
/// Policy for rate limiting API requests.
/// </summary>
public class RateLimitingPolicy
{
    private readonly ILogger<RateLimitingPolicy> _logger;
    private readonly RateLimitingOptions _options;
    private readonly IRateLimiter _rateLimiter;
    private readonly SemaphoreSlim _semaphore;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitingPolicy"/> class.
    /// </summary>
    public RateLimitingPolicy(
        ILogger<RateLimitingPolicy> logger,
        RateLimitingOptions options,
        IRateLimiter rateLimiter)
    {
        _logger = logger;
        _options = options;
        _rateLimiter = rateLimiter;
        _semaphore = new SemaphoreSlim(options.MaxConcurrentRequests, options.MaxConcurrentRequests);
    }

    /// <summary>
    /// Executes an action with rate limiting applied.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(string key, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        // Wait for semaphore (concurrent request limit)
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Check rate limit
            var allowed = await _rateLimiter.TryAcquireAsync(
                key,
                _options.MaxRequestsPerWindow,
                _options.TimeWindow,
                cancellationToken);

            if (!allowed)
            {
                throw new RateLimitExceededException(
                    $"Rate limit exceeded for key '{key}': {_options.MaxRequestsPerWindow} requests per {_options.TimeWindow}");
            }

            return await action(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Executes an action with rate limiting applied (void return).
    /// </summary>
    public async Task ExecuteAsync(string key, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(key, async ct =>
        {
            await action(ct);
            return 0; // Dummy return value
        }, cancellationToken);
    }

    /// <summary>
    /// Gets the number of seconds to wait before retrying (if rate limited).
    /// </summary>
    public async Task<int?> GetRetryAfterSecondsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _rateLimiter.GetRetryAfterAsync(
            key,
            _options.MaxRequestsPerWindow,
            _options.TimeWindow,
            cancellationToken);
    }

    /// <summary>
    /// Gets the rate limiting options.
    /// </summary>
    public RateLimitingOptions Options => _options;
}

/// <summary>
/// Options for rate limiting policy.
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// Maximum number of concurrent requests.
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// Maximum number of requests allowed per time window.
    /// </summary>
    public int MaxRequestsPerWindow { get; set; } = 100;

    /// <summary>
    /// Time window for rate limiting.
    /// </summary>
    public TimeSpan TimeWindow { get; set; } = TimeSpan.FromMinutes(1);
}
