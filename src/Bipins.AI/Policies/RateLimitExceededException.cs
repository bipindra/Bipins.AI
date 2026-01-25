namespace Bipins.AI.Runtime.Policies;

/// <summary>
/// Exception thrown when rate limit is exceeded.
/// </summary>
public class RateLimitExceededException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class.
    /// </summary>
    public RateLimitExceededException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class.
    /// </summary>
    public RateLimitExceededException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
