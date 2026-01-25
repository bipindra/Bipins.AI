namespace Bipins.AI.Runtime.Policies;

/// <summary>
/// Flags for logging behavior.
/// </summary>
[Flags]
public enum LoggingFlags
{
    /// <summary>
    /// No logging.
    /// </summary>
    None = 0,

    /// <summary>
    /// Log requests.
    /// </summary>
    LogRequests = 1,

    /// <summary>
    /// Log responses.
    /// </summary>
    LogResponses = 2,

    /// <summary>
    /// Log embeddings.
    /// </summary>
    LogEmbeddings = 4,

    /// <summary>
    /// Log all.
    /// </summary>
    All = LogRequests | LogResponses | LogEmbeddings
}
