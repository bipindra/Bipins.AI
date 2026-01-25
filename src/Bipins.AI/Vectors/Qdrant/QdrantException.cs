namespace Bipins.AI.Vectors.Qdrant;

/// <summary>
/// Exception thrown by Qdrant connector.
/// </summary>
public class QdrantException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QdrantException"/> class.
    /// </summary>
    public QdrantException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QdrantException"/> class.
    /// </summary>
    public QdrantException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// HTTP status code if available.
    /// </summary>
    public int? StatusCode { get; init; }
}

