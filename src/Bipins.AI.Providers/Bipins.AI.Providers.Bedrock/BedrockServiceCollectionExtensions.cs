using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Bipins.AI.Core.DependencyInjection;
using Bipins.AI.Core.Models;

namespace Bipins.AI.Providers.Bedrock;

/// <summary>
/// Extension methods for registering AWS Bedrock services.
/// </summary>
public static class BedrockServiceCollectionExtensions
{
    /// <summary>
    /// Adds AWS Bedrock services.
    /// </summary>
    public static IBipinsAIBuilder AddBedrock(this IBipinsAIBuilder builder, Action<BedrockOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddSingleton<BedrockChatModel>();
        builder.Services.AddSingleton<BedrockChatModelStreaming>();
        builder.Services.AddSingleton<IChatModel>(sp => sp.GetRequiredService<BedrockChatModel>());
        builder.Services.AddSingleton<IChatModelStreaming>(sp => sp.GetRequiredService<BedrockChatModelStreaming>());

        return builder;
    }
}

