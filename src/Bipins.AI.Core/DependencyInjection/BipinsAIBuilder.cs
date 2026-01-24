using Microsoft.Extensions.DependencyInjection;

namespace Bipins.AI.Core.DependencyInjection;

/// <summary>
/// Default implementation of IBipinsAIBuilder.
/// </summary>
internal class BipinsAIBuilder : IBipinsAIBuilder
{
    public IServiceCollection Services { get; }

    public BipinsAIBuilder(IServiceCollection services)
    {
        Services = services;
    }
}
