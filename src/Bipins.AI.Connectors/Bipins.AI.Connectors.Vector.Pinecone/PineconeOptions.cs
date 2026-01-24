namespace Bipins.AI.Connectors.Vector.Pinecone;

/// <summary>
/// Options for Pinecone vector store.
/// </summary>
public class PineconeOptions
{
    /// <summary>
    /// Pinecone API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Pinecone environment/region (e.g., "us-east-1-aws").
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Default index name.
    /// </summary>
    public string DefaultIndexName { get; set; } = "default";

    /// <summary>
    /// Vector dimension size.
    /// </summary>
    public int VectorSize { get; set; } = 1536;

    /// <summary>
    /// Metric type (cosine, euclidean, dotproduct).
    /// </summary>
    public string Metric { get; set; } = "cosine";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
}
