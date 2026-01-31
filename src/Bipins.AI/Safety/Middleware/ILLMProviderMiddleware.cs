using Bipins.AI.Core.Models;

namespace Bipins.AI.Safety.Middleware;

/// <summary>
/// Interface for middleware that intercepts LLM provider calls.
/// </summary>
public interface ILLMProviderMiddleware
{
    /// <summary>
    /// Invoked before the LLM provider processes a chat request.
    /// </summary>
    /// <param name="request">The chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Modified request or the original request.</returns>
    Task<ChatRequest> OnRequestAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoked after the LLM provider returns a chat response.
    /// </summary>
    /// <param name="request">The original chat request.</param>
    /// <param name="response">The chat response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Modified response or the original response.</returns>
    Task<ChatResponse> OnResponseAsync(ChatRequest request, ChatResponse response, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoked before generating an embedding.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Modified text or the original text.</returns>
    Task<string> OnEmbeddingRequestAsync(string text, CancellationToken cancellationToken = default);
}
