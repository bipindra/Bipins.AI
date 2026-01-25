namespace Bipins.AI.Providers.Anthropic;

/// <summary>
/// Exception thrown by Anthropic connector.
/// </summary>
public class AnthropicException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicException"/> class.
    /// </summary>
    public AnthropicException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicException"/> class.
    /// </summary>
    public AnthropicException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

