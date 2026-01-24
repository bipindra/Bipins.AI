namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Contract for loading documents from a source.
/// </summary>
public interface IDocumentLoader
{
    /// <summary>
    /// Loads a document from the specified source URI.
    /// </summary>
    /// <param name="sourceUri">The source URI (file path, URL, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded document.</returns>
    Task<Document> LoadAsync(string sourceUri, CancellationToken cancellationToken = default);
}
