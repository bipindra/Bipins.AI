namespace Bipins.AI.Connectors.Llm.Bedrock;

/// <summary>
/// Options for AWS Bedrock connector.
/// </summary>
public class BedrockOptions
{
    /// <summary>
    /// AWS Region (e.g., "us-east-1").
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// AWS Access Key ID (optional if using IAM roles).
    /// </summary>
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// AWS Secret Access Key (optional if using IAM roles).
    /// </summary>
    public string? SecretAccessKey { get; set; }

    /// <summary>
    /// Default model ID (e.g., "anthropic.claude-3-5-sonnet-20241022-v2:0").
    /// </summary>
    public string DefaultModelId { get; set; } = "anthropic.claude-3-5-sonnet-20241022-v2:0";

    /// <summary>
    /// Default embedding model ID (if supported).
    /// </summary>
    public string? DefaultEmbeddingModelId { get; set; }

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}
