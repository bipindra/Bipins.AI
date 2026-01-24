namespace Bipins.AI.Providers.AzureOpenAI;

/// <summary>
/// Options for Azure OpenAI connector.
/// </summary>
public class AzureOpenAiOptions
{
    /// <summary>
    /// Azure OpenAI endpoint (e.g., https://your-resource.openai.azure.com/).
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Default deployment name for chat models.
    /// </summary>
    public string DefaultChatDeploymentName { get; set; } = "gpt-35-turbo";

    /// <summary>
    /// Default deployment name for embedding models.
    /// </summary>
    public string DefaultEmbeddingDeploymentName { get; set; } = "text-embedding-ada-002";

    /// <summary>
    /// API version (defaults to 2024-02-15-preview).
    /// </summary>
    public string ApiVersion { get; set; } = "2024-02-15-preview";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}

