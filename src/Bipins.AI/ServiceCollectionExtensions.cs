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
using Bipins.AI.Vector;
using Bipins.AI.Caching;
using Bipins.AI.Runtime.Policies;
using Bipins.AI.Runtime.Routing;
using Bipins.AI.Runtime.Pipeline;
using Bipins.AI.Runtime.Rag;
using Bipins.AI.Runtime.CostTracking;
using Bipins.AI.Ingestion;
using Bipins.AI.Ingestion.Strategies;
using Bipins.AI.Providers;
using Bipins.AI.Providers.OpenAI;
using Bipins.AI.Providers.Anthropic;
using Bipins.AI.Providers.AzureOpenAI;
using Bipins.AI.Providers.Bedrock;
using Bipins.AI.Vectors.Qdrant;
using Bipins.AI.Vectors.Pinecone;
using Bipins.AI.Vectors.Weaviate;
using Bipins.AI.Vectors.Milvus;
using Bipins.AI.Agents;
using Bipins.AI.Agents.Memory;
using Bipins.AI.Agents.Planning;
using Bipins.AI.Agents.Tools;
using Bipins.AI.Agents.Tools.BuiltIn;
using Microsoft.Extensions.Logging;

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
        });

        // Register cache - requires IDistributedCache to be registered by the consumer
        // If IDistributedCache is not registered, this will throw at runtime
        services.AddSingleton<ICache>(sp =>
        {
            var distributedCache = sp.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DistributedCacheAdapter>>();
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CacheOptions>>();
            return new DistributedCacheAdapter(distributedCache, logger, options.Value.KeyPrefix);
        });

        // Register rate limiter (use memory-based implementation)
        services.AddSingleton<IRateLimiter, MemoryRateLimiter>();

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
        services.AddSingleton<IChunkingStrategy, FixedSizeChunkingStrategy>();
        services.AddSingleton<IChunkingStrategy, SentenceAwareChunkingStrategy>();
        services.AddSingleton<IChunkingStrategy, ParagraphChunkingStrategy>();
        services.AddSingleton<IChunkingStrategy, MarkdownAwareChunkingStrategy>();
        
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
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<OpenAiChatModel>();
        builder.Services.AddSingleton<OpenAiEmbeddingModel>();
        builder.Services.AddSingleton<OpenAiChatModelStreaming>();
        builder.Services.AddSingleton<IChatModel>(sp => sp.GetRequiredService<OpenAiChatModel>());
        builder.Services.AddSingleton<IEmbeddingModel>(sp => sp.GetRequiredService<OpenAiEmbeddingModel>());
        builder.Services.AddSingleton<IChatModelStreaming>(sp => sp.GetRequiredService<OpenAiChatModelStreaming>());
        builder.Services.AddSingleton<ILLMProvider, OpenAiLLMProvider>();

        return builder;
    }

    /// <summary>
    /// Adds Anthropic Claude services.
    /// </summary>
    public static IBipinsAIBuilder AddAnthropic(this IBipinsAIBuilder builder, Action<AnthropicOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<AnthropicChatModel>();
        builder.Services.AddSingleton<AnthropicChatModelStreaming>();
        builder.Services.AddSingleton<IChatModel>(sp => sp.GetRequiredService<AnthropicChatModel>());
        builder.Services.AddSingleton<IChatModelStreaming>(sp => sp.GetRequiredService<AnthropicChatModelStreaming>());
        builder.Services.AddSingleton<ILLMProvider, AnthropicLLMProvider>();

        return builder;
    }

    /// <summary>
    /// Adds Azure OpenAI services.
    /// </summary>
    public static IBipinsAIBuilder AddAzureOpenAI(this IBipinsAIBuilder builder, Action<AzureOpenAiOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<AzureOpenAiChatModel>();
        builder.Services.AddSingleton<AzureOpenAiChatModelStreaming>();
        builder.Services.AddSingleton<AzureOpenAiEmbeddingModel>();
        builder.Services.AddSingleton<IChatModel>(sp => sp.GetRequiredService<AzureOpenAiChatModel>());
        builder.Services.AddSingleton<IChatModelStreaming>(sp => sp.GetRequiredService<AzureOpenAiChatModelStreaming>());
        builder.Services.AddSingleton<IEmbeddingModel>(sp => sp.GetRequiredService<AzureOpenAiEmbeddingModel>());
        builder.Services.AddSingleton<ILLMProvider, AzureOpenAiLLMProvider>();

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
        builder.Services.AddSingleton<ILLMProvider, BedrockLLMProvider>();

        return builder;
    }

    /// <summary>
    /// Adds Qdrant vector store services.
    /// </summary>
    public static IBipinsAIBuilder AddQdrant(this IBipinsAIBuilder builder, Action<QdrantOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<QdrantVectorStore>();
        builder.Services.AddSingleton<IVectorStore>(sp => sp.GetRequiredService<QdrantVectorStore>());

        return builder;
    }

    /// <summary>
    /// Adds Pinecone vector store services.
    /// </summary>
    public static IBipinsAIBuilder AddPinecone(this IBipinsAIBuilder builder, Action<PineconeOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<PineconeVectorStore>();
        builder.Services.AddSingleton<IVectorStore>(sp => sp.GetRequiredService<PineconeVectorStore>());

        return builder;
    }

    /// <summary>
    /// Adds Weaviate vector store services.
    /// </summary>
    public static IBipinsAIBuilder AddWeaviate(this IBipinsAIBuilder builder, Action<WeaviateOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<WeaviateVectorStore>();
        builder.Services.AddSingleton<IVectorStore>(sp => sp.GetRequiredService<WeaviateVectorStore>());

        return builder;
    }

    /// <summary>
    /// Adds Milvus vector store services.
    /// </summary>
    public static IBipinsAIBuilder AddMilvus(this IBipinsAIBuilder builder, Action<MilvusOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<MilvusVectorStore>();
        builder.Services.AddSingleton<IVectorStore>(sp => sp.GetRequiredService<MilvusVectorStore>());

        return builder;
    }

    /// <summary>
    /// Adds Bipins.AI Agents support.
    /// </summary>
    public static IBipinsAIBuilder AddBipinsAIAgents(this IBipinsAIBuilder builder, Action<AgentOptions>? configureDefault = null)
    {
        // Register tool registry - will be populated when tools are registered
        builder.Services.AddSingleton<IToolRegistry>(sp =>
        {
            var registry = new DefaultToolRegistry(sp.GetService<ILogger<DefaultToolRegistry>>());
            // Register all IToolExecutor instances
            var tools = sp.GetServices<IToolExecutor>();
            foreach (var tool in tools)
            {
                registry.RegisterTool(tool);
            }
            return registry;
        });

        // Register agent registry
        builder.Services.AddSingleton<IAgentRegistry>(sp =>
        {
            var registry = new DefaultAgentRegistry(sp.GetService<ILogger<DefaultAgentRegistry>>());
            // Register all IAgent instances
            var agents = sp.GetServices<IAgent>();
            foreach (var agent in agents)
            {
                registry.RegisterAgent(agent);
            }
            return registry;
        });

        // Register default planner (LLM-based)
        builder.Services.AddSingleton<IAgentPlanner, LLMPlanner>();

        // Register default memory (in-memory, can be overridden)
        builder.Services.AddSingleton<IAgentMemory, InMemoryAgentMemory>();

        // Configure default agent options if provided
        if (configureDefault != null)
        {
            builder.Services.Configure(configureDefault);
        }

        return builder;
    }

    /// <summary>
    /// Registers an agent with the specified name and configuration.
    /// </summary>
    public static IBipinsAIBuilder AddAgent(this IBipinsAIBuilder builder, string name, Action<AgentOptions> configure)
    {
        var agentId = name.ToLowerInvariant().Replace(" ", "-");

        // Configure agent-specific options
        builder.Services.Configure<AgentOptions>(name, configure);

        // Register the agent instance
        builder.Services.AddSingleton<IAgent>(sp =>
        {
            var optionsSnapshot = sp.GetRequiredService<IOptionsSnapshot<AgentOptions>>();
            var options = optionsSnapshot.Get(name);
            options.Name = string.IsNullOrEmpty(options.Name) ? name : options.Name;

            var llmProvider = sp.GetRequiredService<ILLMProvider>();
            var toolRegistry = sp.GetRequiredService<IToolRegistry>();
            var memory = sp.GetService<IAgentMemory>();
            var planner = sp.GetService<IAgentPlanner>();
            var logger = sp.GetService<ILogger<DefaultAgent>>();

            return new DefaultAgent(agentId, options, llmProvider, toolRegistry, memory, planner, logger);
        });

        return builder;
    }

    /// <summary>
    /// Registers a tool executor.
    /// </summary>
    public static IBipinsAIBuilder AddTool(this IBipinsAIBuilder builder, IToolExecutor tool)
    {
        builder.Services.AddSingleton(tool);
        return builder;
    }

    /// <summary>
    /// Registers the calculator tool.
    /// </summary>
    public static IBipinsAIBuilder AddCalculatorTool(this IBipinsAIBuilder builder)
    {
        builder.Services.AddSingleton<IToolExecutor, CalculatorTool>();
        return builder;
    }

    /// <summary>
    /// Registers the vector search tool (requires IVectorStore and IEmbeddingModel).
    /// </summary>
    public static IBipinsAIBuilder AddVectorSearchTool(this IBipinsAIBuilder builder, string collectionName = "documents")
    {
        builder.Services.AddSingleton<IToolExecutor>(sp =>
        {
            var vectorStore = sp.GetRequiredService<IVectorStore>();
            var embeddingModel = sp.GetRequiredService<IEmbeddingModel>();
            var logger = sp.GetService<ILogger<VectorSearchTool>>();
            return new VectorSearchTool(vectorStore, embeddingModel, collectionName, logger);
        });
        return builder;
    }

    /// <summary>
    /// Configures agent memory to use vector store (requires IVectorStore and IEmbeddingModel).
    /// </summary>
    public static IBipinsAIBuilder UseVectorStoreMemory(this IBipinsAIBuilder builder, string collectionName = "agent_memory")
    {
        builder.Services.AddSingleton<IAgentMemory>(sp =>
        {
            var vectorStore = sp.GetRequiredService<IVectorStore>();
            var embeddingModel = sp.GetRequiredService<IEmbeddingModel>();
            var logger = sp.GetService<ILogger<VectorStoreAgentMemory>>();
            return new VectorStoreAgentMemory(vectorStore, embeddingModel, collectionName, logger);
        });
        return builder;
    }
}
