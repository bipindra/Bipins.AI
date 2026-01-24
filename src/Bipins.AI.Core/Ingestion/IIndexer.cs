namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Contract for indexing chunks into a vector store.
/// </summary>
public interface IIndexer
{
    /// <summary>
    /// Indexes chunks by generating embeddings and storing them in the vector store.
    /// </summary>
    /// <param name="chunks">The chunks to index.</param>
    /// <param name="options">Indexing options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The indexing result.</returns>
    Task<IndexResult> IndexAsync(IEnumerable<Chunk> chunks, IndexOptions options, CancellationToken cancellationToken = default);
}
