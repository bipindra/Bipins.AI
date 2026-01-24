using Bipins.AI.Core.Ingestion;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Ingestion;

/// <summary>
/// Orchestrates the ingestion pipeline: Load → Extract → Chunk → Enrich → Index.
/// </summary>
public class IngestionPipeline
{
    private readonly ILogger<IngestionPipeline> _logger;
    private readonly IDocumentLoader _loader;
    private readonly ITextExtractor _extractor;
    private readonly IChunker _chunker;
    private readonly IMetadataEnricher _enricher;
    private readonly IIndexer _indexer;

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionPipeline"/> class.
    /// </summary>
    public IngestionPipeline(
        ILogger<IngestionPipeline> logger,
        IDocumentLoader loader,
        ITextExtractor extractor,
        IChunker chunker,
        IMetadataEnricher enricher,
        IIndexer indexer)
    {
        _logger = logger;
        _loader = loader;
        _extractor = extractor;
        _chunker = chunker;
        _enricher = enricher;
        _indexer = indexer;
    }

    /// <summary>
    /// Ingests a document through the full pipeline.
    /// </summary>
    public async Task<IndexResult> IngestAsync(
        string sourceUri,
        IndexOptions options,
        ChunkOptions? chunkOptions = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ingestion pipeline for {SourceUri}", sourceUri);

        // Load
        var document = await _loader.LoadAsync(sourceUri, cancellationToken);

        // Extract
        var text = await _extractor.ExtractAsync(document, cancellationToken);

        // Chunk
        chunkOptions ??= new ChunkOptions();
        var chunks = await _chunker.ChunkAsync(text, chunkOptions, cancellationToken);

        // Enrich and Index
        var enrichedChunks = new List<Chunk>();
        foreach (var chunk in chunks)
        {
            var metadata = await _enricher.EnrichAsync(chunk, document, cancellationToken);
            var enrichedChunk = chunk with { Metadata = metadata };
            enrichedChunks.Add(enrichedChunk);
        }

        // Index
        var result = await _indexer.IndexAsync(enrichedChunks, options, cancellationToken);

        _logger.LogInformation(
            "Ingestion completed: {ChunksIndexed} chunks indexed, {VectorsCreated} vectors created",
            result.ChunksIndexed,
            result.VectorsCreated);

        return result;
    }
}
