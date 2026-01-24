using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Bipins.AI.Core.DependencyInjection;
using Bipins.AI.Core.Vector;

namespace Bipins.AI.Connectors.Vector.Qdrant;

/// <summary>
/// Extension methods for registering Qdrant services.
/// </summary>
public static class QdrantServiceCollectionExtensions
{
    /// <summary>
    /// Adds Qdrant vector store services.
    /// </summary>
    public static IBipinsAIBuilder AddQdrant(this IBipinsAIBuilder builder, Action<QdrantOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddHttpClient<QdrantVectorStore>();
        builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();

        return builder;
    }
}

