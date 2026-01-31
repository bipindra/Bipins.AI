namespace Bipins.AI.Safety;

/// <summary>
/// Interface for content moderation services.
/// </summary>
public interface IContentModerator
{
    /// <summary>
    /// Moderates the specified content.
    /// </summary>
    /// <param name="content">The content to moderate.</param>
    /// <param name="contentType">The content type (e.g., "text/plain", "text/html").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Moderation result.</returns>
    Task<ModerationResult> ModerateAsync(
        string content, 
        string contentType = "text/plain", 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the content is safe.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <param name="contentType">The content type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if content is safe, false otherwise.</returns>
    Task<bool> IsSafeAsync(
        string content, 
        string contentType = "text/plain", 
        CancellationToken cancellationToken = default);
}
