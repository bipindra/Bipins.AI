using Bipins.AI.Core.Ingestion;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Ingestion;

/// <summary>
/// Default metadata enricher that adds common metadata.
/// </summary>
public class DefaultMetadataEnricher : IMetadataEnricher
{
    private readonly ILogger<DefaultMetadataEnricher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMetadataEnricher"/> class.
    /// </summary>
    public DefaultMetadataEnricher(ILogger<DefaultMetadataEnricher> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<Dictionary<string, object>> EnrichAsync(Chunk chunk, Document document, CancellationToken cancellationToken = default)
    {
        var metadata = new Dictionary<string, object>(chunk.Metadata ?? new Dictionary<string, object>())
        {
            ["sourceUri"] = document.SourceUri,
            ["mimeType"] = document.MimeType,
            ["indexedAt"] = DateTime.UtcNow.ToString("O")
        };

        // Extract title from document if available
        if (document.Metadata?.TryGetValue("title", out var title) == true)
        {
            metadata["title"] = title;
        }

        // Preserve heading if present
        if (chunk.Metadata?.TryGetValue("heading", out var heading) == true)
        {
            metadata["heading"] = heading;
        }

        _logger.LogDebug("Enriched metadata for chunk {ChunkId}", chunk.Id);
        return Task.FromResult(metadata);
    }
}
