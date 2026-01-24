namespace Bipins.AI.Core.Rag;

/// <summary>
/// Contract for retrieving relevant chunks for RAG.
/// </summary>
public interface IRetriever
{
    /// <summary>
    /// Retrieves relevant chunks based on the query.
    /// </summary>
    /// <param name="request">The retrieval request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The retrieval result.</returns>
    Task<RetrieveResult> RetrieveAsync(RetrieveRequest request, CancellationToken cancellationToken = default);
}
