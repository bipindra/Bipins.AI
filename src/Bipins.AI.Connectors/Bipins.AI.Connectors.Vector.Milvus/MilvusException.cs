namespace Bipins.AI.Connectors.Vector.Milvus;

/// <summary>
/// Exception thrown by Milvus connector.
/// </summary>
public class MilvusException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MilvusException"/> class.
    /// </summary>
    public MilvusException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MilvusException"/> class.
    /// </summary>
    public MilvusException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
