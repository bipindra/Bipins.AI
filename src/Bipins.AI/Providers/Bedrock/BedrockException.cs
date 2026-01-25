namespace Bipins.AI.Providers.Bedrock;

/// <summary>
/// Exception thrown by AWS Bedrock connector.
/// </summary>
public class BedrockException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BedrockException"/> class.
    /// </summary>
    public BedrockException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BedrockException"/> class.
    /// </summary>
    public BedrockException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

