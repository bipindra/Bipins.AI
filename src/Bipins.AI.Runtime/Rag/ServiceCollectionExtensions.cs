using Microsoft.Extensions.DependencyInjection;
using Bipins.AI.Core.Rag;

namespace Bipins.AI.Runtime.Rag;

/// <summary>
/// Extension methods for registering RAG services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds RAG services.
    /// </summary>
    public static IServiceCollection AddBipinsAIRag(this IServiceCollection services)
    {
        services.AddSingleton<IRetriever, VectorRetriever>();
        services.AddSingleton<IRagComposer, DefaultRagComposer>();

        return services;
    }
}
