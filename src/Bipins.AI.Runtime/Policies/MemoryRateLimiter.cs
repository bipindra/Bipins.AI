using Bipins.AI.Core.Runtime.Policies;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Runtime.Policies;

/// <summary>
/// In-memory rate limiter implementation (single instance).
/// </summary>
public class MemoryRateLimiter : IRateLimiter
{
    private readonly ILogger<MemoryRateLimiter> _logger;
    private readonly Dictionary<string, RateLimitWindow> _rateLimitWindows = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryRateLimiter"/> class.
    /// </summary>
    public MemoryRateLimiter(ILogger<MemoryRateLimiter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> TryAcquireAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var rateLimitKey = $"{key}:{limit}:{window.TotalSeconds}";

            if (!_rateLimitWindows.TryGetValue(rateLimitKey, out var rateLimitWindow))
            {
                rateLimitWindow = new RateLimitWindow(limit, window);
                _rateLimitWindows[rateLimitKey] = rateLimitWindow;
            }

            // Clean up old timestamps outside the window
            rateLimitWindow.Cleanup(now);

            // Check if we've exceeded the limit
            if (rateLimitWindow.RequestTimestamps.Count >= limit)
            {
                _logger.LogDebug(
                    "Rate limit exceeded for key {Key}: {Count}/{Limit} requests in {Window}",
                    key,
                    rateLimitWindow.RequestTimestamps.Count,
                    limit,
                    window);
                return Task.FromResult(false);
            }

            // Add current request timestamp
            rateLimitWindow.RequestTimestamps.Enqueue(now);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<int?> GetRetryAfterAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var rateLimitKey = $"{key}:{limit}:{window.TotalSeconds}";

            if (!_rateLimitWindows.TryGetValue(rateLimitKey, out var rateLimitWindow))
            {
                return Task.FromResult<int?>(null);
            }

            rateLimitWindow.Cleanup(now);

            if (rateLimitWindow.RequestTimestamps.Count >= limit && rateLimitWindow.RequestTimestamps.Count > 0)
            {
                var oldestTimestamp = rateLimitWindow.RequestTimestamps.Peek();
                var waitTime = window - (now - oldestTimestamp);
                if (waitTime > TimeSpan.Zero)
                {
                    return Task.FromResult<int?>((int)Math.Ceiling(waitTime.TotalSeconds));
                }
            }

            return Task.FromResult<int?>(null);
        }
    }

    /// <inheritdoc />
    public Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var keysToRemove = _rateLimitWindows.Keys
                .Where(k => k.StartsWith($"{key}:", StringComparison.Ordinal))
                .ToList();

            foreach (var k in keysToRemove)
            {
                _rateLimitWindows.Remove(k);
            }

            _logger.LogDebug("Reset rate limit for key {Key}", key);
            return Task.CompletedTask;
        }
    }

    private class RateLimitWindow
    {
        public Queue<DateTime> RequestTimestamps { get; } = new();
        private readonly int _limit;
        private readonly TimeSpan _window;

        public RateLimitWindow(int limit, TimeSpan window)
        {
            _limit = limit;
            _window = window;
        }

        public void Cleanup(DateTime now)
        {
            var cutoffTime = now - _window;
            while (RequestTimestamps.Count > 0 && RequestTimestamps.Peek() < cutoffTime)
            {
                RequestTimestamps.Dequeue();
            }
        }
    }
}
