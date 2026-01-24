namespace Bipins.AI.Connectors.Vector.Weaviate;

/// <summary>
/// Exception thrown by Weaviate connector.
/// </summary>
public class WeaviateException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeaviateException"/> class.
    /// </summary>
    public WeaviateException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WeaviateException"/> class.
    /// </summary>
    public WeaviateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
