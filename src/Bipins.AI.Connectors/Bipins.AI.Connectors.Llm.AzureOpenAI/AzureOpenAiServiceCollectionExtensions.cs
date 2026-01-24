using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Bipins.AI.Core.DependencyInjection;
using Bipins.AI.Core.Models;

namespace Bipins.AI.Connectors.Llm.AzureOpenAI;

/// <summary>
/// Extension methods for registering Azure OpenAI services.
/// </summary>
public static class AzureOpenAiServiceCollectionExtensions
{
    /// <summary>
    /// Adds Azure OpenAI services.
    /// </summary>
    public static IBipinsAIBuilder AddAzureOpenAI(this IBipinsAIBuilder builder, Action<AzureOpenAiOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddHttpClient<AzureOpenAiChatModel>();
        builder.Services.AddHttpClient<AzureOpenAiEmbeddingModel>();
        builder.Services.AddSingleton<IChatModel, AzureOpenAiChatModel>();
        builder.Services.AddSingleton<IEmbeddingModel, AzureOpenAiEmbeddingModel>();

        return builder;
    }
}
