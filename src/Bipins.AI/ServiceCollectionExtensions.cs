using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Bipins.AI.Core;
using Bipins.AI.Core.DependencyInjection;
using Bipins.AI.Core.Ingestion;
using Bipins.AI.Core.Rag;
using Bipins.AI.Core.Runtime.Policies;
using Bipins.AI.Core.CostTracking;
using Bipins.AI.Core.Models;
using Bipins.AI.Core.Vector;
using Bipins.AI.Runtime.Caching;
using Bipins.AI.Runtime.Policies;
using Bipins.AI.Runtime.Routing;
using Bipins.AI.Runtime.Pipeline;
using Bipins.AI.Runtime.Rag;
using Bipins.AI.Runtime.CostTracking;
using Bipins.AI.Ingestion;
using Bipins.AI.Ingestion.Strategies;
using Bipins.AI.Providers.OpenAI;
using Bipins.AI.Providers.Anthropic;
using Bipins.AI.Providers.AzureOpenAI;
using Bipins.AI.Providers.Bedrock;
using Bipins.AI.Vectors.Qdrant;
using Bipins.AI.Vectors.Pinecone;
using Bipins.AI.Vectors.Weaviate;
using Bipins.AI.Vectors.Milvus;

namespace Bipins.AI;

/// <summary>
/// Extension methods for registering Bipins.AI services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Bipins.AI services.
    /// </summary>
    public static IBipinsAIBuilder AddBipinsAI(this IServiceCollection services, Action<BipinsAIOptions>? configure = null)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }

        return new BipinsAIBuilder(services);
    }

    /// <summary>
    /// Adds Bipins.AI Runtime services.
    /// </summary>
    public static IServiceCollection AddBipinsAIRuntime(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddSingleton<IAiPolicyProvider, DefaultPolicyProvider>();
        services.AddSingleton<IModelRouter, DefaultModelRouter>();
        services.AddSingleton<StepRetryHandler>();
        services.AddSingleton<PipelineRunner>();
        services.AddSingleton<RateLimitingPolicy>();
        services.AddSingleton<ThrottlingPolicy>();

        // Configure cache options
        services.Configure<CacheOptions>(options =>
        {
            var defaultTtl = configuration?.GetValue<int>("Cache:DefaultTtlHours", 1);
            options.DefaultTtl = TimeSpan.FromHours(defaultTtl ?? 1);
            options.KeyPrefix = configuration?.GetValue<string>("Cache:KeyPrefix") ?? "bipins:cache:";
            options.RedisConnectionString = configuration?.GetConnectionString("Redis");
        });

        // Register cache (use Redis if connection string is provided, otherwise use memory)
        var redisConnectionString = configuration?.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            // Register Redis connection
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
                StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString));
            
            // Register Redis cache
            services.AddSingleton<ICache>(sp =>
            {
                var redis = sp.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RedisCache>>();
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CacheOptions>>();
                return new RedisCache(redis, logger, options.Value.KeyPrefix);
            });
        }
        else
        {
            services.AddSingleton<ICache, MemoryCache>();
        }

        // Register rate limiter (use distributed if Redis connection string is provided, otherwise use memory)
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddSingleton<IRateLimiter>(sp =>
            {
                var redis = sp.GetService<StackExchange.Redis.IConnectionMultiplexer>();
                return new DistributedRateLimiter(
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DistributedRateLimiter>>(),
                    redis);
            });
        }
        else
        {
            services.AddSingleton<IRateLimiter, MemoryRateLimiter>();
        }

        // Register cost tracking
        services.AddSingleton<ICostCalculator, DefaultCostCalculator>();
        services.AddSingleton<ICostTracker, InMemoryCostTracker>();

        return services;
    }

    /// <summary>
    /// Adds Bipins.AI Ingestion services.
    /// </summary>
    public static IServiceCollection AddBipinsAIIngestion(this IServiceCollection services)
    {
        // Register chunking strategies
        services.AddSingleton<FixedSizeChunkingStrategy>();
        services.AddSingleton<SentenceAwareChunkingStrategy>();
        services.AddSingleton<ParagraphChunkingStrategy>();
        services.AddSingleton<MarkdownAwareChunkingStrategy>();
        
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

    /// <summary>
    /// Adds RAG services.
    /// </summary>
    public static IServiceCollection AddBipinsAIRag(this IServiceCollection services)
    {
        services.AddSingleton<IRetriever, VectorRetriever>();
        services.AddSingleton<IRagComposer, DefaultRagComposer>();

        return services;
    }

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

    /// <summary>
    /// Adds Azure OpenAI services.
    /// </summary>
    public static IBipinsAIBuilder AddAzureOpenAI(this IBipinsAIBuilder builder, Action<AzureOpenAiOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddHttpClient<AzureOpenAiChatModel>();
        builder.Services.AddHttpClient<AzureOpenAiChatModelStreaming>();
        builder.Services.AddHttpClient<AzureOpenAiEmbeddingModel>();
        builder.Services.AddSingleton<IChatModel, AzureOpenAiChatModel>();
        builder.Services.AddSingleton<IChatModelStreaming, AzureOpenAiChatModelStreaming>();
        builder.Services.AddSingleton<IEmbeddingModel, AzureOpenAiEmbeddingModel>();

        return builder;
    }

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
