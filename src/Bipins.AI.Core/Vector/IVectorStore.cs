namespace Bipins.AI.Core.Vector;

/// <summary>
/// Contract for vector storage operations.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Upserts vector records into the store.
    /// </summary>
    /// <param name="request">The upsert request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task UpsertAsync(VectorUpsertRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the vector store for similar vectors.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The query response with matching records.</returns>
    Task<VectorQueryResponse> QueryAsync(VectorQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes vector records from the store.
    /// </summary>
    /// <param name="request">The delete request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task DeleteAsync(VectorDeleteRequest request, CancellationToken cancellationToken = default);
}
