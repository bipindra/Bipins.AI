using Bipins.AI.Core.Ingestion;

namespace Bipins.AI.Worker.Ingestion;

/// <summary>
/// Represents an ingestion job.
/// </summary>
/// <param name="SourceUri">Source URI of the document to ingest.</param>
/// <param name="Options">Indexing options.</param>
/// <param name="ChunkOptions">Chunking options.</param>
public record IngestionJob(
    string SourceUri,
    IndexOptions Options,
    ChunkOptions? ChunkOptions = null);
