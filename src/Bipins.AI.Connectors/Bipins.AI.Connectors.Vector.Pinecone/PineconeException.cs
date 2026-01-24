namespace Bipins.AI.Connectors.Vector.Pinecone;

/// <summary>
/// Exception thrown by Pinecone connector.
/// </summary>
public class PineconeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PineconeException"/> class.
    /// </summary>
    public PineconeException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PineconeException"/> class.
    /// </summary>
    public PineconeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
