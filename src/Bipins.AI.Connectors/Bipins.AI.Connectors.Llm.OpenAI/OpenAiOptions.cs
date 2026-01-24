namespace Bipins.AI.Connectors.Llm.OpenAI;

/// <summary>
/// Options for OpenAI connector.
/// </summary>
public class OpenAiOptions
{
    /// <summary>
    /// OpenAI API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL (defaults to https://api.openai.com/v1).
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>
    /// Default chat model ID (e.g., "gpt-4", "gpt-3.5-turbo").
    /// </summary>
    public string DefaultChatModelId { get; set; } = "gpt-3.5-turbo";

    /// <summary>
    /// Default embedding model ID (e.g., "text-embedding-ada-002").
    /// </summary>
    public string DefaultEmbeddingModelId { get; set; } = "text-embedding-ada-002";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}
