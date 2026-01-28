using Bipins.AI.Core.Models;

namespace Bipins.AI.Providers;

/// <summary>
/// Provider interface for LLM operations using Core.Models types.
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// The default chat model used by this provider (backed by provider-specific options).
    /// </summary>
    IChatModel CurrentModel { get; }

    /// <summary>
    /// Generates a chat completion response.
    /// </summary>
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a streaming chat completion response.
    /// </summary>
    IAsyncEnumerable<ChatResponseChunk> ChatStreamAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for the given text.
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
