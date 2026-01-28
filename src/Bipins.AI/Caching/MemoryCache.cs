using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Runtime.Caching;

/// <summary>
/// Cache implementation based on IDistributedCache.
/// </summary>
public class DistributedCacheAdapter : ICache
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<DistributedCacheAdapter> _logger;
    private readonly string _keyPrefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedCacheAdapter"/> class.
    /// </summary>
    public DistributedCacheAdapter(
        IDistributedCache distributedCache,
        ILogger<DistributedCacheAdapter> logger,
        string? keyPrefix = null)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _keyPrefix = keyPrefix ?? "bipins:cache:";
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var fullKey = GetFullKey(key);
            var bytes = await _distributedCache.GetAsync(fullKey, cancellationToken);

            if (bytes == null || bytes.Length == 0)
            {
                _logger.LogDebug("Cache miss for key {Key}", key);
                return null;
            }

            var json = System.Text.Encoding.UTF8.GetString(bytes);
            var deserialized = JsonSerializer.Deserialize<T>(json);
            _logger.LogDebug("Cache hit for key {Key}", key);
            return deserialized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from cache for key {Key}", key);
            return null; // Fail gracefully
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var fullKey = GetFullKey(key);
            var json = JsonSerializer.Serialize(value);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var expiry = ttl ?? TimeSpan.FromHours(1);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            };

            await _distributedCache.SetAsync(fullKey, bytes, options, cancellationToken);
            _logger.LogDebug("Cached value for key {Key} with TTL {Ttl}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in cache for key {Key}", key);
            // Fail silently - cache is not critical
        }
    }

    private string GetFullKey(string key)
    {
        return $"{_keyPrefix}{key}";
    }
}
