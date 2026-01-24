using Bipins.AI.Core.Runtime.Policies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
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
        services.AddSingleton<Caching.ICache, Caching.MemoryCache>();
        services.AddSingleton<Pipeline.StepRetryHandler>();
        services.AddSingleton<Pipeline.PipelineRunner>();
        services.AddSingleton<RateLimitingPolicy>();
        services.AddSingleton<ThrottlingPolicy>();

        // Register rate limiter (use distributed if Redis connection string is provided, otherwise use memory)
        var redisConnectionString = configuration?.GetConnectionString("Redis");
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
