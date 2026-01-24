namespace Bipins.AI.Connectors.Llm.OpenAI;

/// <summary>
/// Exception thrown by OpenAI connector.
/// </summary>
public class OpenAiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiException"/> class.
    /// </summary>
    public OpenAiException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiException"/> class.
    /// </summary>
    public OpenAiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// HTTP status code if available.
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>
    /// Retry-After header value if available.
    /// </summary>
    public int? RetryAfterSeconds { get; init; }
}
