using Microsoft.Extensions.DependencyInjection;
using Bipins.AI.Core.DependencyInjection;

namespace Bipins.AI.Core;

/// <summary>
/// Extension methods for registering Bipins.AI services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Bipins.AI services.
    /// </summary>
    public static IBipinsAIBuilder AddBipinsAI(this IServiceCollection services, Action<BipinsAIOptions>? configure = null)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }

        return new BipinsAIBuilder(services);
    }
}
