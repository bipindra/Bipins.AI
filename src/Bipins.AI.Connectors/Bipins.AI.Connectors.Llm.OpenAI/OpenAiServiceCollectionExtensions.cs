using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Bipins.AI.Core.DependencyInjection;
using Bipins.AI.Core.Models;

namespace Bipins.AI.Connectors.Llm.OpenAI;

/// <summary>
/// Extension methods for registering OpenAI services.
/// </summary>
public static class OpenAiServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenAI services.
    /// </summary>
    public static IBipinsAIBuilder AddOpenAI(this IBipinsAIBuilder builder, Action<OpenAiOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddHttpClient<OpenAiChatModel>();
        builder.Services.AddHttpClient<OpenAiEmbeddingModel>();
        builder.Services.AddHttpClient<OpenAiChatModelStreaming>();
        builder.Services.AddSingleton<IChatModel, OpenAiChatModel>();
        builder.Services.AddSingleton<IEmbeddingModel, OpenAiEmbeddingModel>();
        builder.Services.AddSingleton<IChatModelStreaming, OpenAiChatModelStreaming>();

        return builder;
    }
}
