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

    /// <summary>
    /// Ingests multiple documents through the full pipeline in batch.
    /// </summary>
    public async Task<BatchIndexResult> IngestBatchAsync(
        IEnumerable<string> sourceUris,
        IndexOptions options,
        ChunkOptions? chunkOptions = null,
        int? maxConcurrency = null,
        CancellationToken cancellationToken = default)
    {
        var sourceUriList = sourceUris.ToList();
        _logger.LogInformation("Starting batch ingestion pipeline for {Count} documents", sourceUriList.Count);

        maxConcurrency ??= Environment.ProcessorCount;
        var semaphore = new SemaphoreSlim(maxConcurrency.Value);

        var results = new List<IndexResult>();
        var errors = new List<BatchIngestionError>();

        var tasks = sourceUriList.Select(async sourceUri =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = await IngestAsync(sourceUri, options, chunkOptions, cancellationToken);
                lock (results)
                {
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest document {SourceUri}", sourceUri);
                lock (errors)
                {
                    errors.Add(new BatchIngestionError(sourceUri, ex.Message));
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        var totalChunks = results.Sum(r => r.ChunksIndexed);
        var totalVectors = results.Sum(r => r.VectorsCreated);

        _logger.LogInformation(
            "Batch ingestion completed: {SuccessCount}/{TotalCount} documents, {TotalChunks} chunks indexed, {TotalVectors} vectors created",
            results.Count,
            sourceUriList.Count,
            totalChunks,
            totalVectors);

        return new BatchIndexResult(
            results,
            errors,
            totalChunks,
            totalVectors);
    }
}

/// <summary>
/// Result of batch ingestion operation.
/// </summary>
/// <param name="Results">Individual ingestion results.</param>
/// <param name="Errors">Errors encountered during batch processing.</param>
/// <param name="TotalChunksIndexed">Total number of chunks indexed across all documents.</param>
/// <param name="TotalVectorsCreated">Total number of vectors created across all documents.</param>
public record BatchIndexResult(
    List<IndexResult> Results,
    List<BatchIngestionError> Errors,
    int TotalChunksIndexed,
    int TotalVectorsCreated);

/// <summary>
/// Error encountered during batch ingestion.
/// </summary>
/// <param name="SourceUri">The source URI that failed.</param>
/// <param name="ErrorMessage">The error message.</param>
public record BatchIngestionError(
    string SourceUri,
    string ErrorMessage);
