namespace Bipins.AI.Core.CostTracking;

/// <summary>
/// Represents a cost record for tracking usage and costs.
/// </summary>
/// <param name="Id">Unique identifier for the cost record.</param>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="OperationType">Type of operation (Chat, Embedding, Storage, Query).</param>
/// <param name="Provider">The provider name (e.g., OpenAI, Anthropic).</param>
/// <param name="ModelId">The model identifier used.</param>
/// <param name="TokensUsed">Number of tokens used (if applicable).</param>
/// <param name="PromptTokens">Number of prompt tokens (if applicable).</param>
/// <param name="CompletionTokens">Number of completion tokens (if applicable).</param>
/// <param name="StorageBytes">Storage bytes used (if applicable).</param>
/// <param name="ApiCalls">Number of API calls made.</param>
/// <param name="Cost">The cost in USD.</param>
/// <param name="Timestamp">When the operation occurred.</param>
/// <param name="Metadata">Additional metadata.</param>
public record CostRecord(
    string Id,
    string TenantId,
    CostOperationType OperationType,
    string Provider,
    string? ModelId = null,
    int? TokensUsed = null,
    int? PromptTokens = null,
    int? CompletionTokens = null,
    long? StorageBytes = null,
    int ApiCalls = 1,
    decimal Cost = 0,
    DateTimeOffset Timestamp = default,
    Dictionary<string, object>? Metadata = null)
{
    public DateTimeOffset Timestamp { get; init; } = Timestamp == default ? DateTimeOffset.UtcNow : Timestamp;
}

/// <summary>
/// Type of cost operation.
/// </summary>
public enum CostOperationType
{
    /// <summary>
    /// Chat completion operation.
    /// </summary>
    Chat,

    /// <summary>
    /// Embedding generation operation.
    /// </summary>
    Embedding,

    /// <summary>
    /// Vector storage operation.
    /// </summary>
    Storage,

    /// <summary>
    /// Vector query operation.
    /// </summary>
    Query,

    /// <summary>
    /// Document ingestion operation.
    /// </summary>
    Ingestion
}
