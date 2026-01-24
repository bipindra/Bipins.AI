using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Bipins.AI.Core.DependencyInjection;
using Bipins.AI.Core.Models;

namespace Bipins.AI.Providers.Anthropic;

/// <summary>
/// Extension methods for registering Anthropic services.
/// </summary>
public static class AnthropicServiceCollectionExtensions
{
    /// <summary>
    /// Adds Anthropic Claude services.
    /// </summary>
    public static IBipinsAIBuilder AddAnthropic(this IBipinsAIBuilder builder, Action<AnthropicOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddHttpClient<AnthropicChatModel>();
        builder.Services.AddHttpClient<AnthropicChatModelStreaming>();
        builder.Services.AddSingleton<IChatModel, AnthropicChatModel>();
        builder.Services.AddSingleton<IChatModelStreaming, AnthropicChatModelStreaming>();

        return builder;
    }
}

