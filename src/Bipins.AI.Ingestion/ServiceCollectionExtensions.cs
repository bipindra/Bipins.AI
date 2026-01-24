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
        // Register chunking strategies
        services.AddSingleton<Strategies.FixedSizeChunkingStrategy>();
        services.AddSingleton<Strategies.SentenceAwareChunkingStrategy>();
        services.AddSingleton<Strategies.ParagraphChunkingStrategy>();
        services.AddSingleton<Strategies.MarkdownAwareChunkingStrategy>();
        
        // Register strategy factory
        services.AddSingleton<IChunkingStrategyFactory, DefaultChunkingStrategyFactory>();
        
        // Register chunker (uses strategy factory)
        services.AddSingleton<IChunker, MarkdownAwareChunker>();
        
        services.AddSingleton<IDocumentLoader, TextDocumentLoader>();
        services.AddSingleton<ITextExtractor, MarkdownTextExtractor>();
        services.AddSingleton<IMetadataEnricher, DefaultMetadataEnricher>();
        services.AddSingleton<IIndexer, DefaultIndexer>();
        services.AddSingleton<IDocumentVersionManager, VectorStoreDocumentVersionManager>();
        services.AddSingleton<ITenantManager, InMemoryTenantManager>();
        services.AddSingleton<ITenantQuotaEnforcer, TenantQuotaEnforcer>();
        services.AddSingleton<IngestionPipeline>();

        return services;
    }
}
