namespace Bipins.AI.Core.CostTracking;

/// <summary>
/// Interface for calculating costs based on provider pricing.
/// </summary>
public interface ICostCalculator
{
    /// <summary>
    /// Calculates the cost for a chat completion operation.
    /// </summary>
    /// <param name="provider">The provider name.</param>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="promptTokens">Number of prompt tokens.</param>
    /// <param name="completionTokens">Number of completion tokens.</param>
    /// <returns>The cost in USD.</returns>
    decimal CalculateChatCost(string provider, string modelId, int promptTokens, int completionTokens);

    /// <summary>
    /// Calculates the cost for an embedding operation.
    /// </summary>
    /// <param name="provider">The provider name.</param>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="tokens">Number of tokens.</param>
    /// <returns>The cost in USD.</returns>
    decimal CalculateEmbeddingCost(string provider, string modelId, int tokens);

    /// <summary>
    /// Calculates the cost for storage.
    /// </summary>
    /// <param name="storageBytes">Storage bytes used.</param>
    /// <returns>The cost in USD.</returns>
    decimal CalculateStorageCost(long storageBytes);

    /// <summary>
    /// Calculates the cost for a query operation.
    /// </summary>
    /// <param name="provider">The provider name.</param>
    /// <param name="queries">Number of queries.</param>
    /// <returns>The cost in USD.</returns>
    decimal CalculateQueryCost(string provider, int queries);
}
