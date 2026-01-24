using Bipins.AI.Core.Runtime.Policies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Bipins.AI.Runtime.Caching;
using Bipins.AI.Runtime.Policies;
using Bipins.AI.Runtime.Routing;

namespace Bipins.AI.Runtime;

/// <summary>
/// Extension methods for registering Runtime services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Bipins.AI Runtime services.
    /// </summary>
    public static IServiceCollection AddBipinsAIRuntime(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddSingleton<Policies.IAiPolicyProvider, Policies.DefaultPolicyProvider>();
        services.AddSingleton<Routing.IModelRouter, Routing.DefaultModelRouter>();
        services.AddSingleton<Pipeline.StepRetryHandler>();
        services.AddSingleton<Pipeline.PipelineRunner>();
        services.AddSingleton<RateLimitingPolicy>();
        services.AddSingleton<ThrottlingPolicy>();

        // Configure cache options
        services.Configure<Caching.CacheOptions>(options =>
        {
            var defaultTtl = configuration?.GetValue<int>("Cache:DefaultTtlHours", 1);
            options.DefaultTtl = TimeSpan.FromHours(defaultTtl ?? 1);
            options.KeyPrefix = configuration?.GetValue<string>("Cache:KeyPrefix") ?? "bipins:cache:";
            options.RedisConnectionString = configuration?.GetConnectionString("Redis");
        });

        // Register cache (use Redis if connection string is provided, otherwise use memory)
        var redisConnectionString = configuration?.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            // Register Redis connection
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
                StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString));
            
            // Register Redis cache
            services.AddSingleton<Caching.ICache>(sp =>
            {
                var redis = sp.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Caching.RedisCache>>();
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Caching.CacheOptions>>();
                return new Caching.RedisCache(redis, logger, options.Value.KeyPrefix);
            });
        }
        else
        {
            services.AddSingleton<Caching.ICache, Caching.MemoryCache>();
        }

        // Register rate limiter (use distributed if Redis connection string is provided, otherwise use memory)
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddSingleton<IRateLimiter>(sp =>
                new DistributedRateLimiter(
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DistributedRateLimiter>>(),
                    redisConnectionString));
        }
        else
        {
            services.AddSingleton<IRateLimiter, MemoryRateLimiter>();
        }

        return services;
    }
}
