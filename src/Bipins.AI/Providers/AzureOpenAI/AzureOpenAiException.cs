namespace Bipins.AI.Providers.AzureOpenAI;

/// <summary>
/// Exception thrown by Azure OpenAI connector.
/// </summary>
public class AzureOpenAiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAiException"/> class.
    /// </summary>
    public AzureOpenAiException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAiException"/> class.
    /// </summary>
    public AzureOpenAiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

