using Microsoft.Extensions.DependencyInjection;

namespace Bipins.AI.Core.DependencyInjection;

/// <summary>
/// Builder interface for Bipins.AI services.
/// </summary>
public interface IBipinsAIBuilder
{
    /// <summary>
    /// The service collection.
    /// </summary>
    IServiceCollection Services { get; }
}
