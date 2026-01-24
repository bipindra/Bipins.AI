namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Contract for extracting text from documents.
/// </summary>
public interface ITextExtractor
{
    /// <summary>
    /// Extracts text content from a document.
    /// </summary>
    /// <param name="document">The document to extract text from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The extracted text.</returns>
    Task<string> ExtractAsync(Document document, CancellationToken cancellationToken = default);
}
