namespace Bipins.AI.Core.Contracts;

/// <summary>
/// Status of an AI operation.
/// </summary>
public enum OutputStatus
{
    /// <summary>
    /// Operation completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Operation completed with errors.
    /// </summary>
    Error,

    /// <summary>
    /// Operation completed partially (e.g., some chunks failed).
    /// </summary>
    Partial
}
