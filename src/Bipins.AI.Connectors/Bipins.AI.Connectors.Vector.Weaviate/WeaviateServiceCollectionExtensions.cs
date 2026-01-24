using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Bipins.AI.Core.DependencyInjection;
using Bipins.AI.Core.Vector;

namespace Bipins.AI.Connectors.Vector.Weaviate;

/// <summary>
/// Extension methods for registering Weaviate services.
/// </summary>
public static class WeaviateServiceCollectionExtensions
{
    /// <summary>
    /// Adds Weaviate vector store services.
    /// </summary>
    public static IBipinsAIBuilder AddWeaviate(this IBipinsAIBuilder builder, Action<WeaviateOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddHttpClient<WeaviateVectorStore>();
        builder.Services.AddSingleton<IVectorStore, WeaviateVectorStore>();

        return builder;
    }
}
