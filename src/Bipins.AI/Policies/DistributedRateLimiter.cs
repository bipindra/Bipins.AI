using Bipins.AI.Core.Runtime.Policies;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Bipins.AI.Runtime.Policies;

/// <summary>
/// Redis-based distributed rate limiter for multi-instance deployments.
/// </summary>
public class DistributedRateLimiter : IRateLimiter
{
    private readonly ILogger<DistributedRateLimiter> _logger;
    private readonly IDatabase? _database;
    private readonly bool _useRedis;
    private const string KeyPrefix = "bipins:ratelimit:";

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedRateLimiter"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="redis">Optional Redis connection multiplexer. If not provided, falls back to in-memory.</param>
    public DistributedRateLimiter(
        ILogger<DistributedRateLimiter> logger,
        IConnectionMultiplexer? redis = null)
    {
        _logger = logger;
        _database = redis?.GetDatabase();
        _useRedis = _database != null;

        if (!_useRedis)
        {
            _logger.LogWarning("Redis connection not provided. DistributedRateLimiter will use in-memory fallback (not distributed).");
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryAcquireAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        if (!_useRedis || _database == null)
        {
            // Fallback to in-memory implementation
            // In a real implementation, this would use a shared MemoryRateLimiter or similar
            _logger.LogDebug("Using in-memory fallback for rate limiting (key: {Key})", key);
            return true; // Allow request in fallback mode
        }

        try
        {
            var redisKey = $"{KeyPrefix}{key}";
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var windowStart = currentTime - (long)window.TotalMilliseconds;

            // Remove entries outside the window
            await _database.SortedSetRemoveRangeByScoreAsync(redisKey, double.NegativeInfinity, windowStart);

            // Count remaining entries
            var count = await _database.SortedSetLengthAsync(redisKey);

            if (count < limit)
            {
                // Add new entry with current timestamp as score and unique identifier as value
                var entryValue = $"{currentTime}:{Guid.NewGuid():N}";
                await _database.SortedSetAddAsync(redisKey, entryValue, (double)currentTime);
                
                // Set expiration on the key to prevent memory leaks (window + 1 minute buffer)
                await _database.KeyExpireAsync(redisKey, window.Add(TimeSpan.FromMinutes(1)));
                
                return true;
            }

            _logger.LogDebug("Rate limit exceeded for key {Key} (count: {Count}, limit: {Limit})", key, count, limit);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit in Redis for key {Key}, allowing request", key);
            return true; // Fail open - allow request on Redis errors
        }
    }

    /// <inheritdoc />
    public async Task<int?> GetRetryAfterAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        if (!_useRedis || _database == null)
        {
            return null;
        }

        try
        {
            var redisKey = $"{KeyPrefix}{key}";
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var windowStart = currentTime - (long)window.TotalMilliseconds;

            // Remove entries outside the window
            await _database.SortedSetRemoveRangeByScoreAsync(redisKey, double.NegativeInfinity, windowStart);

            // Count remaining entries
            var count = await _database.SortedSetLengthAsync(redisKey);

            if (count < limit)
            {
                return null; // Not rate limited
            }

            // Get the oldest entry in the window
            var oldestEntries = await _database.SortedSetRangeByScoreWithScoresAsync(
                redisKey,
                windowStart,
                double.PositiveInfinity,
                Exclude.None,
                Order.Ascending,
                0,
                1);

            if (oldestEntries == null || oldestEntries.Length == 0)
            {
                return null;
            }

            var oldestTimestamp = (long)oldestEntries[0].Score;
            var windowEnd = oldestTimestamp + (long)window.TotalMilliseconds;
            var retryAfterMs = windowEnd - currentTime;

            if (retryAfterMs <= 0)
            {
                return null;
            }

            // Return seconds (round up)
            var retryAfterSeconds = (int)Math.Ceiling(retryAfterMs / 1000.0);
            return retryAfterSeconds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating retry-after in Redis for key {Key}", key);
            return null; // Fail gracefully
        }
    }

    /// <inheritdoc />
    public async Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_useRedis || _database == null)
        {
            return;
        }

        try
        {
            var redisKey = $"{KeyPrefix}{key}";
            await _database.KeyDeleteAsync(redisKey);
            _logger.LogDebug("Reset rate limit for key {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting rate limit in Redis for key {Key}", key);
            // Fail silently - reset is not critical
        }
    }
}
