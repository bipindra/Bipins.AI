using Microsoft.Extensions.Logging;

namespace Bipins.AI.Runtime.Policies;

/// <summary>
/// Policy for rate limiting API requests.
/// </summary>
public class RateLimitingPolicy
{
    private readonly ILogger<RateLimitingPolicy> _logger;
    private readonly RateLimitingOptions _options;
    private readonly SemaphoreSlim _semaphore;
    private readonly Queue<DateTime> _requestTimestamps;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitingPolicy"/> class.
    /// </summary>
    public RateLimitingPolicy(
        ILogger<RateLimitingPolicy> logger,
        RateLimitingOptions options)
    {
        _logger = logger;
        _options = options;
        _semaphore = new SemaphoreSlim(options.MaxConcurrentRequests, options.MaxConcurrentRequests);
        _requestTimestamps = new Queue<DateTime>();
    }

    /// <summary>
    /// Executes an action with rate limiting applied.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        // Wait for semaphore (concurrent request limit)
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Wait for rate limit (requests per time window)
            await WaitForRateLimitAsync(cancellationToken);

            // Record request timestamp
            lock (_requestTimestamps)
            {
                _requestTimestamps.Enqueue(DateTime.UtcNow);
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
    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async ct =>
        {
            await action(ct);
            return 0; // Dummy return value
        }, cancellationToken);
    }

    private async Task WaitForRateLimitAsync(CancellationToken cancellationToken)
    {
        DateTime? oldestTimestamp = null;
        int requestCount = 0;

        lock (_requestTimestamps)
        {
            // Remove timestamps outside the time window
            var cutoffTime = DateTime.UtcNow - _options.TimeWindow;
            while (_requestTimestamps.Count > 0 && _requestTimestamps.Peek() < cutoffTime)
            {
                _requestTimestamps.Dequeue();
            }

            requestCount = _requestTimestamps.Count;
            if (_requestTimestamps.Count > 0)
            {
                oldestTimestamp = _requestTimestamps.Peek();
            }
        }

        // If we've exceeded the rate limit, wait until the oldest request expires
        if (requestCount >= _options.MaxRequestsPerWindow && oldestTimestamp.HasValue)
        {
            var waitTime = _options.TimeWindow - (DateTime.UtcNow - oldestTimestamp.Value);
            if (waitTime > TimeSpan.Zero)
            {
                _logger.LogDebug(
                    "Rate limit reached ({Count}/{Max} requests). Waiting {WaitTime}ms",
                    requestCount,
                    _options.MaxRequestsPerWindow,
                    waitTime.TotalMilliseconds);

                await Task.Delay(waitTime, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Gets the number of seconds to wait before retrying (if rate limited).
    /// </summary>
    public int? GetRetryAfterSeconds()
    {
        lock (_requestTimestamps)
        {
            if (_requestTimestamps.Count >= _options.MaxRequestsPerWindow && _requestTimestamps.Count > 0)
            {
                var oldestTimestamp = _requestTimestamps.Peek();
                var waitTime = _options.TimeWindow - (DateTime.UtcNow - oldestTimestamp);
                if (waitTime > TimeSpan.Zero)
                {
                    return (int)Math.Ceiling(waitTime.TotalSeconds);
                }
            }
            return null;
        }
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
