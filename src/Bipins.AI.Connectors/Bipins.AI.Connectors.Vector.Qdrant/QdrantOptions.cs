namespace Bipins.AI.Connectors.Vector.Qdrant;

/// <summary>
/// Options for Qdrant vector store.
/// </summary>
public class QdrantOptions
{
    /// <summary>
    /// Qdrant endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:6333";

    /// <summary>
    /// Optional API key for authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Default collection name.
    /// </summary>
    public string DefaultCollectionName { get; set; } = "default";

    /// <summary>
    /// Vector size (dimension).
    /// </summary>
    public int VectorSize { get; set; } = 1536;

    /// <summary>
    /// Distance metric (Cosine, Euclidean, Dot).
    /// </summary>
    public string Distance { get; set; } = "Cosine";

    /// <summary>
    /// Whether to create collection if missing on startup.
    /// </summary>
    public bool CreateCollectionIfMissing { get; set; } = true;
}
