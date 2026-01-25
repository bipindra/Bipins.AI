namespace Bipins.AI.Vectors.Milvus;

/// <summary>
/// Options for Milvus vector store.
/// </summary>
public class MilvusOptions
{
    /// <summary>
    /// Milvus endpoint (host:port).
    /// </summary>
    public string Endpoint { get; set; } = "localhost:19530";

    /// <summary>
    /// Default collection name.
    /// </summary>
    public string DefaultCollectionName { get; set; } = "default";

    /// <summary>
    /// Vector dimension size.
    /// </summary>
    public int VectorSize { get; set; } = 1536;

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
}

