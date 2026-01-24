using Microsoft.Extensions.DependencyInjection;
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
    public static IServiceCollection AddBipinsAIRuntime(this IServiceCollection services)
    {
        services.AddSingleton<Policies.IAiPolicyProvider, Policies.DefaultPolicyProvider>();
        services.AddSingleton<Routing.IModelRouter, Routing.DefaultModelRouter>();
        services.AddSingleton<Caching.ICache, Caching.MemoryCache>();
        services.AddSingleton<Pipeline.StepRetryHandler>();
        services.AddSingleton<Pipeline.PipelineRunner>();
        services.AddSingleton<RateLimitingPolicy>();
        services.AddSingleton<ThrottlingPolicy>();

        return services;
    }
}
