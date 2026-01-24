namespace Bipins.AI.Core.Ingestion;

/// <summary>
/// Contract for enriching chunk metadata.
/// </summary>
public interface IMetadataEnricher
{
    /// <summary>
    /// Enriches chunk metadata with additional information.
    /// </summary>
    /// <param name="chunk">The chunk to enrich.</param>
    /// <param name="document">The source document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of enriched metadata.</returns>
    Task<Dictionary<string, object>> EnrichAsync(Chunk chunk, Document document, CancellationToken cancellationToken = default);
}
