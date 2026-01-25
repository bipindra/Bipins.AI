using Microsoft.Extensions.Logging;

namespace Bipins.AI.Runtime.Policies;

/// <summary>
/// Policy for throttling requests based on backpressure signals.
/// </summary>
public class ThrottlingPolicy
{
    private readonly ILogger<ThrottlingPolicy> _logger;
    private readonly ThrottlingOptions _options;
    private readonly SemaphoreSlim _semaphore;
    private int _currentLoad = 0;
    private DateTime _lastThrottleTime = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThrottlingPolicy"/> class.
    /// </summary>
    public ThrottlingPolicy(
        ILogger<ThrottlingPolicy> logger,
        ThrottlingOptions options)
    {
        _logger = logger;
        _options = options;
        _semaphore = new SemaphoreSlim(options.MaxConcurrentRequests, options.MaxConcurrentRequests);
    }

    /// <summary>
    /// Executes an action with throttling applied.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        // Wait for semaphore (concurrent request limit)
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Apply adaptive throttling based on current load
            await ApplyAdaptiveThrottlingAsync(cancellationToken);

            Interlocked.Increment(ref _currentLoad);
            try
            {
                return await action(cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _currentLoad);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Executes an action with throttling applied (void return).
    /// </summary>
    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async ct =>
        {
            await action(ct);
            return 0; // Dummy return value
        }, cancellationToken);
    }

    /// <summary>
    /// Reports an error to adjust throttling behavior.
    /// </summary>
    public void ReportError()
    {
        _lastThrottleTime = DateTime.UtcNow;
        _logger.LogWarning("Error reported, throttling will be more aggressive");
    }

    private async Task ApplyAdaptiveThrottlingAsync(CancellationToken cancellationToken)
    {
        var currentLoad = Volatile.Read(ref _currentLoad);
        var timeSinceLastThrottle = DateTime.UtcNow - _lastThrottleTime;

        // If we recently had an error, apply more aggressive throttling
        if (timeSinceLastThrottle < _options.ThrottleRecoveryTime)
        {
            var throttleDelay = _options.BaseThrottleDelay * (1.0 + (1.0 - timeSinceLastThrottle.TotalSeconds / _options.ThrottleRecoveryTime.TotalSeconds));
            await Task.Delay(TimeSpan.FromMilliseconds(throttleDelay), cancellationToken);
        }
        // If current load is high, apply progressive throttling
        else if (currentLoad >= _options.HighLoadThreshold)
        {
            var loadFactor = (double)currentLoad / _options.MaxConcurrentRequests;
            var throttleDelay = _options.BaseThrottleDelay * loadFactor;
            await Task.Delay(TimeSpan.FromMilliseconds(throttleDelay), cancellationToken);
        }
    }
}

/// <summary>
/// Options for throttling policy.
/// </summary>
public class ThrottlingOptions
{
    /// <summary>
    /// Maximum number of concurrent requests.
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// Threshold for considering load as "high".
    /// </summary>
    public int HighLoadThreshold { get; set; } = 8;

    /// <summary>
    /// Base throttle delay in milliseconds.
    /// </summary>
    public double BaseThrottleDelay { get; set; } = 100;

    /// <summary>
    /// Time to recover from throttling after an error.
    /// </summary>
    public TimeSpan ThrottleRecoveryTime { get; set; } = TimeSpan.FromSeconds(30);
}
