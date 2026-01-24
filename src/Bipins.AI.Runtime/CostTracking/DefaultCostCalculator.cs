using Bipins.AI.Core.CostTracking;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Runtime.CostTracking;

/// <summary>
/// Default cost calculator with provider pricing.
/// </summary>
public class DefaultCostCalculator : ICostCalculator
{
    private readonly ILogger<DefaultCostCalculator> _logger;
    
    // Pricing per 1K tokens (in USD)
    private static readonly Dictionary<string, Dictionary<string, (decimal Input, decimal Output)>> ChatPricing = new()
    {
        ["OpenAI"] = new()
        {
            ["gpt-4"] = (0.03m, 0.06m),
            ["gpt-4-turbo"] = (0.01m, 0.03m),
            ["gpt-3.5-turbo"] = (0.0015m, 0.002m),
            ["gpt-3.5-turbo-16k"] = (0.003m, 0.004m),
        },
        ["Anthropic"] = new()
        {
            ["claude-3-opus"] = (0.015m, 0.075m),
            ["claude-3-sonnet"] = (0.003m, 0.015m),
            ["claude-3-haiku"] = (0.00025m, 0.00125m),
        },
        ["Azure"] = new()
        {
            ["gpt-4"] = (0.03m, 0.06m),
            ["gpt-4-turbo"] = (0.01m, 0.03m),
            ["gpt-35-turbo"] = (0.0015m, 0.002m),
        },
        ["Bedrock"] = new()
        {
            ["anthropic.claude-3-opus"] = (0.015m, 0.075m),
            ["anthropic.claude-3-sonnet"] = (0.003m, 0.015m),
            ["anthropic.claude-3-haiku"] = (0.00025m, 0.00125m),
        }
    };

    private static readonly Dictionary<string, Dictionary<string, decimal>> EmbeddingPricing = new()
    {
        ["OpenAI"] = new()
        {
            ["text-embedding-ada-002"] = 0.0001m,
            ["text-embedding-3-small"] = 0.00002m,
            ["text-embedding-3-large"] = 0.00013m,
        },
        ["Azure"] = new()
        {
            ["text-embedding-ada-002"] = 0.0001m,
        }
    };

    // Storage pricing: $0.10 per GB per month
    private const decimal StorageCostPerGBPerMonth = 0.10m;
    private const long BytesPerGB = 1_000_000_000;

    // Query pricing: typically free for vector stores, but some providers charge
    private static readonly Dictionary<string, decimal> QueryPricing = new()
    {
        ["Qdrant"] = 0m, // Free
        ["Pinecone"] = 0m, // Free for basic queries
        ["Weaviate"] = 0m, // Free
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCostCalculator"/> class.
    /// </summary>
    public DefaultCostCalculator(ILogger<DefaultCostCalculator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public decimal CalculateChatCost(string provider, string modelId, int promptTokens, int completionTokens)
    {
        if (!ChatPricing.TryGetValue(provider, out var models))
        {
            _logger.LogWarning("Unknown provider for chat cost calculation: {Provider}", provider);
            return 0m;
        }

        // Try exact match first
        if (models.TryGetValue(modelId, out var pricing))
        {
            var inputCost = (promptTokens / 1000m) * pricing.Input;
            var outputCost = (completionTokens / 1000m) * pricing.Output;
            return inputCost + outputCost;
        }

        // Try partial match (e.g., "gpt-4-32k" matches "gpt-4")
        var matchedModel = models.Keys.FirstOrDefault(k => modelId.StartsWith(k, StringComparison.OrdinalIgnoreCase));
        if (matchedModel != null && models.TryGetValue(matchedModel, out var matchedPricing))
        {
            var inputCost = (promptTokens / 1000m) * matchedPricing.Input;
            var outputCost = (completionTokens / 1000m) * matchedPricing.Output;
            return inputCost + outputCost;
        }

        _logger.LogWarning("Unknown model for chat cost calculation: {Provider}/{ModelId}", provider, modelId);
        return 0m;
    }

    /// <inheritdoc />
    public decimal CalculateEmbeddingCost(string provider, string modelId, int tokens)
    {
        if (!EmbeddingPricing.TryGetValue(provider, out var models))
        {
            _logger.LogWarning("Unknown provider for embedding cost calculation: {Provider}", provider);
            return 0m;
        }

        if (models.TryGetValue(modelId, out var pricePer1K))
        {
            return (tokens / 1000m) * pricePer1K;
        }

        _logger.LogWarning("Unknown model for embedding cost calculation: {Provider}/{ModelId}", provider, modelId);
        return 0m;
    }

    /// <inheritdoc />
    public decimal CalculateStorageCost(long storageBytes)
    {
        // Calculate monthly cost, then prorate to daily
        var gb = storageBytes / (decimal)BytesPerGB;
        var monthlyCost = gb * StorageCostPerGBPerMonth;
        var dailyCost = monthlyCost / 30m; // Approximate daily cost
        return dailyCost;
    }

    /// <inheritdoc />
    public decimal CalculateQueryCost(string provider, int queries)
    {
        if (QueryPricing.TryGetValue(provider, out var pricePerQuery))
        {
            return queries * pricePerQuery;
        }

        _logger.LogWarning("Unknown provider for query cost calculation: {Provider}", provider);
        return 0m;
    }
}
