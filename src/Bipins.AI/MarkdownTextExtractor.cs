using Bipins.AI.Core.Ingestion;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Bipins.AI.Ingestion;

/// <summary>
/// Extracts text from markdown documents, preserving structure.
/// </summary>
public class MarkdownTextExtractor : ITextExtractor
{
    private readonly ILogger<MarkdownTextExtractor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownTextExtractor"/> class.
    /// </summary>
    public MarkdownTextExtractor(ILogger<MarkdownTextExtractor> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string> ExtractAsync(Document document, CancellationToken cancellationToken = default)
    {
        var text = Encoding.UTF8.GetString(document.Content);

        // For markdown, we preserve the structure but could strip formatting if needed
        // For now, just return the text as-is
        if (document.MimeType == "text/markdown")
        {
            _logger.LogDebug("Extracted markdown text ({Length} chars)", text.Length);
            return Task.FromResult(text);
        }

        // For plain text, just return
        if (document.MimeType == "text/plain")
        {
            _logger.LogDebug("Extracted plain text ({Length} chars)", text.Length);
            return Task.FromResult(text);
        }

        // For other types, try UTF-8 decoding
        _logger.LogWarning("Unknown MIME type {MimeType}, attempting UTF-8 decode", document.MimeType);
        return Task.FromResult(text);
    }
}
