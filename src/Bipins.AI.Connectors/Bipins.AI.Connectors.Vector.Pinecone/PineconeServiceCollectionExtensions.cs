using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Bipins.AI.Core.DependencyInjection;
using Bipins.AI.Core.Vector;

namespace Bipins.AI.Connectors.Vector.Pinecone;

/// <summary>
/// Extension methods for registering Pinecone services.
/// </summary>
public static class PineconeServiceCollectionExtensions
{
    /// <summary>
    /// Adds Pinecone vector store services.
    /// </summary>
    public static IBipinsAIBuilder AddPinecone(this IBipinsAIBuilder builder, Action<PineconeOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddHttpClient<PineconeVectorStore>();
        builder.Services.AddSingleton<IVectorStore, PineconeVectorStore>();

        return builder;
    }
}
