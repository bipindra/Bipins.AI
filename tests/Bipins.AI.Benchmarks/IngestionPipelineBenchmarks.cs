using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Bipins.AI.Core.Ingestion;
using Bipins.AI.Ingestion;
using Bipins.AI.Ingestion.Strategies;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bipins.AI.Benchmarks;

/// <summary>
/// Benchmarks for the ingestion pipeline.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class IngestionPipelineBenchmarks
{
    private readonly IngestionPipeline _pipeline;
    private readonly string _smallText;
    private readonly string _mediumText;
    private readonly string _largeText;

    public IngestionPipelineBenchmarks()
    {
        // Create a minimal pipeline for benchmarking
        var loader = new TextDocumentLoader(NullLogger<TextDocumentLoader>.Instance);
        var extractor = new MarkdownTextExtractor(NullLogger<MarkdownTextExtractor>.Instance);
        var strategies = new List<IChunkingStrategy>
        {
            new FixedSizeChunkingStrategy(NullLogger<FixedSizeChunkingStrategy>.Instance),
            new SentenceAwareChunkingStrategy(NullLogger<SentenceAwareChunkingStrategy>.Instance),
            new ParagraphChunkingStrategy(NullLogger<ParagraphChunkingStrategy>.Instance),
            new MarkdownAwareChunkingStrategy(NullLogger<MarkdownAwareChunkingStrategy>.Instance)
        };
        var chunker = new MarkdownAwareChunker(
            NullLogger<MarkdownAwareChunker>.Instance,
            new DefaultChunkingStrategyFactory(
                strategies,
                NullLogger<DefaultChunkingStrategyFactory>.Instance));
        var enricher = new DefaultMetadataEnricher(NullLogger<DefaultMetadataEnricher>.Instance);
        
        // Use a mock indexer for benchmarking
        var indexer = new MockIndexer();
        
        _pipeline = new IngestionPipeline(
            NullLogger<IngestionPipeline>.Instance,
            loader,
            extractor,
            chunker,
            enricher,
            indexer);

        _smallText = GenerateText(1000);
        _mediumText = GenerateText(10000);
        _largeText = GenerateText(100000);
    }

    [Benchmark]
    public async Task IngestSmallDocument()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, _smallText);
        
        try
        {
            var options = new IndexOptions("benchmark", "doc1", null, null);
            await _pipeline.IngestAsync(tempFile, options);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Benchmark]
    public async Task IngestMediumDocument()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, _mediumText);
        
        try
        {
            var options = new IndexOptions("benchmark", "doc2", null, null);
            await _pipeline.IngestAsync(tempFile, options);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Benchmark]
    public async Task IngestLargeDocument()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, _largeText);
        
        try
        {
            var options = new IndexOptions("benchmark", "doc3", null, null);
            await _pipeline.IngestAsync(tempFile, options);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static string GenerateText(int length)
    {
        var words = new[] { "the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog" };
        var random = new Random(42);
        var text = new System.Text.StringBuilder(length);
        
        while (text.Length < length)
        {
            text.Append(words[random.Next(words.Length)]);
            text.Append(' ');
        }
        
        return text.ToString().Substring(0, Math.Min(length, text.Length));
    }

    private class MockIndexer : IIndexer
    {
        public Task<IndexResult> IndexAsync(
            IReadOnlyList<Chunk> chunks,
            IndexOptions options,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new IndexResult(
                ChunksIndexed: chunks.Count,
                VectorsCreated: chunks.Count,
                Errors: null));
        }
    }
}
