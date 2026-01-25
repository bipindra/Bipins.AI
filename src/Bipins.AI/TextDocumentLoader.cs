using Bipins.AI.Core.Ingestion;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Ingestion;

/// <summary>
/// Loads text documents from local file system.
/// </summary>
public class TextDocumentLoader : IDocumentLoader
{
    private readonly ILogger<TextDocumentLoader> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextDocumentLoader"/> class.
    /// </summary>
    public TextDocumentLoader(ILogger<TextDocumentLoader> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Document> LoadAsync(string sourceUri, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourceUri))
        {
            throw new FileNotFoundException($"File not found: {sourceUri}");
        }

        var content = await File.ReadAllBytesAsync(sourceUri, cancellationToken);
        var mimeType = GetMimeType(sourceUri);

        _logger.LogInformation("Loaded document from {SourceUri} ({Size} bytes)", sourceUri, content.Length);

        return new Document(sourceUri, content, mimeType);
    }

    private static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            ".html" => "text/html",
            ".json" => "application/json",
            _ => "application/octet-stream"
        };
    }
}
