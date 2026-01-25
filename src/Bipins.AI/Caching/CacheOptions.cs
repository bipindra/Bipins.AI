namespace Bipins.AI.Runtime.Caching;

/// <summary>
/// Options for cache configuration.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Default TTL for cached items.
    /// </summary>
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Key prefix for cache keys.
    /// </summary>
    public string KeyPrefix { get; set; } = "bipins:cache:";

    /// <summary>
    /// Redis connection string (if using Redis).
    /// </summary>
    public string? RedisConnectionString { get; set; }
}
