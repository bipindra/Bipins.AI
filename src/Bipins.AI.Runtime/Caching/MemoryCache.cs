using Microsoft.Extensions.Logging;

namespace Bipins.AI.Runtime.Caching;

/// <summary>
/// Simple in-memory cache implementation.
/// </summary>
public class MemoryCache : ICache
{
    private readonly ILogger<MemoryCache> _logger;
    private readonly Dictionary<string, CacheEntry> _cache = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCache"/> class.
    /// </summary>
    public MemoryCache(ILogger<MemoryCache> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.ExpiresAt > DateTime.UtcNow)
                {
                    if (entry.Value is T value)
                    {
                        return Task.FromResult<T?>(value);
                    }
                }
                else
                {
                    _cache.Remove(key);
                }
            }
        }

        return Task.FromResult<T?>(null);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class
    {
        var expiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : DateTime.UtcNow.AddHours(1);

        lock (_lock)
        {
            _cache[key] = new CacheEntry(value, expiresAt);
            _logger.LogDebug("Cached value for key {Key} with TTL {Ttl}", key, ttl ?? TimeSpan.FromHours(1));
        }

        return Task.CompletedTask;
    }

    private record CacheEntry(object Value, DateTime ExpiresAt);
}
