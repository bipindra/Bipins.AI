namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Strategy for chunking text.
/// </summary>
public enum ChunkStrategy
{
    /// <summary>
    /// Fixed-size chunks with overlap.
    /// </summary>
    FixedSize,

    /// <summary>
    /// Chunk by markdown headings.
    /// </summary>
    MarkdownAware,

    /// <summary>
    /// Chunk by sentences.
    /// </summary>
    Sentence,

    /// <summary>
    /// Chunk by paragraphs.
    /// </summary>
    Paragraph
}
