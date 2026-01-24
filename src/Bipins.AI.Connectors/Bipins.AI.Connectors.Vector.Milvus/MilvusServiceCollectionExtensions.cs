using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Bipins.AI.Core.DependencyInjection;
using Bipins.AI.Core.Vector;

namespace Bipins.AI.Connectors.Vector.Milvus;

/// <summary>
/// Extension methods for registering Milvus services.
/// </summary>
public static class MilvusServiceCollectionExtensions
{
    /// <summary>
    /// Adds Milvus vector store services.
    /// </summary>
    public static IBipinsAIBuilder AddMilvus(this IBipinsAIBuilder builder, Action<MilvusOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddHttpClient<MilvusVectorStore>();
        builder.Services.AddSingleton<IVectorStore, MilvusVectorStore>();

        return builder;
    }
}
