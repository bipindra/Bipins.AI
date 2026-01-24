namespace Bipins.AI.Connectors.Vector.Weaviate;

/// <summary>
/// Options for Weaviate vector store.
/// </summary>
public class WeaviateOptions
{
    /// <summary>
    /// Weaviate endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:8080";

    /// <summary>
    /// Optional API key for authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Default class name (collection).
    /// </summary>
    public string DefaultClassName { get; set; } = "Document";

    /// <summary>
    /// Vector dimension size.
    /// </summary>
    public int VectorSize { get; set; } = 1536;

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
}
