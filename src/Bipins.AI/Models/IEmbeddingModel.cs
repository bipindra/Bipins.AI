namespace Bipins.AI.Core.Models;

/// <summary>
/// Contract for embedding models.
/// </summary>
public interface IEmbeddingModel
{
    /// <summary>
    /// Generates embeddings for the given inputs.
    /// </summary>
    /// <param name="request">The embedding request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The embedding response.</returns>
    Task<EmbeddingResponse> EmbedAsync(EmbeddingRequest request, CancellationToken cancellationToken = default);
}
