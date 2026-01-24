namespace Bipins.AI.Core.Models;

/// <summary>
/// Contract for chat completion models.
/// </summary>
public interface IChatModel
{
    /// <summary>
    /// Generates a chat completion response.
    /// </summary>
    /// <param name="request">The chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chat response.</returns>
    Task<ChatResponse> GenerateAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
