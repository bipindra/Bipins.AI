namespace Bipins.AI.Connectors.Llm.Anthropic;

/// <summary>
/// Options for Anthropic Claude connector.
/// </summary>
public class AnthropicOptions
{
    /// <summary>
    /// Anthropic API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL (defaults to https://api.anthropic.com/v1).
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.anthropic.com/v1";

    /// <summary>
    /// Default chat model ID (e.g., "claude-3-5-sonnet-20241022", "claude-3-opus-20240229").
    /// </summary>
    public string DefaultChatModelId { get; set; } = "claude-3-5-sonnet-20241022";

    /// <summary>
    /// Default embedding model ID (if Anthropic supports embeddings).
    /// </summary>
    public string? DefaultEmbeddingModelId { get; set; }

    /// <summary>
    /// API version header.
    /// </summary>
    public string ApiVersion { get; set; } = "2023-06-01";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}
