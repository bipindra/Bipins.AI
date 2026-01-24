using Bipins.AI.Core.Models;

namespace Bipins.AI.Runtime.Routing;

/// <summary>
/// Contract for routing model requests to appropriate providers.
/// </summary>
public interface IModelRouter
{
    /// <summary>
    /// Selects a chat model for the given request.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="request">The chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected chat model.</returns>
    Task<IChatModel> SelectChatModelAsync(string tenantId, ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects an embedding model for the given request.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="request">The embedding request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected embedding model.</returns>
    Task<IEmbeddingModel> SelectEmbeddingModelAsync(string tenantId, EmbeddingRequest request, CancellationToken cancellationToken = default);
}
