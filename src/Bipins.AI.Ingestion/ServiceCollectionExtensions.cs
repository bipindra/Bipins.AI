using Microsoft.Extensions.DependencyInjection;
using Bipins.AI.Core.Ingestion;

namespace Bipins.AI.Ingestion;

/// <summary>
/// Extension methods for registering Ingestion services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Bipins.AI Ingestion services.
    /// </summary>
    public static IServiceCollection AddBipinsAIIngestion(this IServiceCollection services)
    {
        services.AddSingleton<IDocumentLoader, TextDocumentLoader>();
        services.AddSingleton<ITextExtractor, MarkdownTextExtractor>();
        services.AddSingleton<IChunker, MarkdownAwareChunker>();
        services.AddSingleton<IMetadataEnricher, DefaultMetadataEnricher>();
        services.AddSingleton<IIndexer, DefaultIndexer>();
        services.AddSingleton<IngestionPipeline>();

        return services;
    }
}
