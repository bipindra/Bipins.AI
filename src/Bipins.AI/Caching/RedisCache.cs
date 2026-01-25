using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Bipins.AI.Runtime.Caching;

/// <summary>
/// Redis-based distributed cache implementation.
/// </summary>
public class RedisCache : ICache
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisCache> _logger;
    private readonly string _keyPrefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisCache"/> class.
    /// </summary>
    public RedisCache(
        IConnectionMultiplexer redis,
        ILogger<RedisCache> logger,
        string? keyPrefix = null)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _logger = logger;
        _keyPrefix = keyPrefix ?? "bipins:cache:";
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var fullKey = GetFullKey(key);
            var value = await _database.StringGetAsync(fullKey);

            if (!value.HasValue)
            {
                _logger.LogDebug("Cache miss for key {Key}", key);
                return null;
            }

            var deserialized = JsonSerializer.Deserialize<T>(value!);
            _logger.LogDebug("Cache hit for key {Key}", key);
            return deserialized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from Redis cache for key {Key}", key);
            return null; // Fail gracefully
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var fullKey = GetFullKey(key);
            var serialized = JsonSerializer.Serialize(value);
            var expiry = ttl ?? TimeSpan.FromHours(1);

            await _database.StringSetAsync(fullKey, serialized, expiry);
            _logger.LogDebug("Cached value for key {Key} with TTL {Ttl}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in Redis cache for key {Key}", key);
            // Fail silently - cache is not critical
        }
    }

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            await _database.KeyDeleteAsync(fullKey);
            _logger.LogDebug("Removed cache entry for key {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from Redis cache for key {Key}", key);
        }
    }

    /// <summary>
    /// Removes all cache entries matching a pattern.
    /// </summary>
    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPattern = GetFullKey(pattern);
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            await foreach (var key in server.KeysAsync(pattern: fullPattern))
            {
                await _database.KeyDeleteAsync(key);
            }
            _logger.LogDebug("Removed cache entries matching pattern {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entries by pattern {Pattern}", pattern);
        }
    }

    private string GetFullKey(string key)
    {
        return $"{_keyPrefix}{key}";
    }
}
