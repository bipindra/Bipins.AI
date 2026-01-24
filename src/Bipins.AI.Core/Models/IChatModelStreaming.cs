namespace Bipins.AI.Core.Models;

/// <summary>
/// Contract for streaming chat completion models.
/// </summary>
public interface IChatModelStreaming
{
    /// <summary>
    /// Generates a streaming chat completion response.
    /// </summary>
    /// <param name="request">The chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of chat response chunks.</returns>
    IAsyncEnumerable<ChatResponseChunk> GenerateStreamAsync(ChatRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a chunk of a streaming chat response.
/// </summary>
/// <param name="Content">The text content chunk.</param>
/// <param name="IsComplete">Whether this is the final chunk.</param>
/// <param name="ModelId">The model identifier used.</param>
/// <param name="Usage">Token usage information (typically only in final chunk).</param>
/// <param name="FinishReason">Reason why generation finished (only in final chunk).</param>
public record ChatResponseChunk(
    string Content,
    bool IsComplete = false,
    string? ModelId = null,
    Usage? Usage = null,
    string? FinishReason = null);
