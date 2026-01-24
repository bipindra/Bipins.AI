using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Bipins.AI.Core.Ingestion;
using Bipins.AI.Ingestion;
using Bipins.AI.Ingestion.Strategies;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bipins.AI.Benchmarks;

/// <summary>
/// Benchmarks for chunking operations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class ChunkingBenchmarks
{
    private readonly FixedSizeChunkingStrategy _fixedSizeStrategy;
    private readonly SentenceAwareChunkingStrategy _sentenceStrategy;
    private readonly ParagraphChunkingStrategy _paragraphStrategy;
    private readonly MarkdownAwareChunkingStrategy _markdownStrategy;
    
    private readonly string _smallText;
    private readonly string _mediumText;
    private readonly string _largeText;
    private readonly string _markdownText;

    public ChunkingBenchmarks()
    {
        _fixedSizeStrategy = new FixedSizeChunkingStrategy(NullLogger<FixedSizeChunkingStrategy>.Instance);
        _sentenceStrategy = new SentenceAwareChunkingStrategy(NullLogger<SentenceAwareChunkingStrategy>.Instance);
        _paragraphStrategy = new ParagraphChunkingStrategy(NullLogger<ParagraphChunkingStrategy>.Instance);
        _markdownStrategy = new MarkdownAwareChunkingStrategy(NullLogger<MarkdownAwareChunkingStrategy>.Instance);

        _smallText = GenerateText(1000); // ~1KB
        _mediumText = GenerateText(10000); // ~10KB
        _largeText = GenerateText(100000); // ~100KB
        _markdownText = GenerateMarkdownText(10000);
    }

    [Benchmark]
    [Arguments(1000)]
    [Arguments(10000)]
    [Arguments(100000)]
    public async Task FixedSizeChunking(int textSize)
    {
        var text = GenerateText(textSize);
        var options = new ChunkOptions(
            MaxSize: 500,
            Overlap: 50);
        await _fixedSizeStrategy.ChunkAsync(text, options);
    }

    [Benchmark]
    [Arguments(1000)]
    [Arguments(10000)]
    [Arguments(100000)]
    public async Task SentenceAwareChunking(int textSize)
    {
        var text = GenerateText(textSize);
        var options = new ChunkOptions(
            MaxSize: 500,
            Overlap: 50);
        await _sentenceStrategy.ChunkAsync(text, options);
    }

    [Benchmark]
    [Arguments(1000)]
    [Arguments(10000)]
    [Arguments(100000)]
    public async Task ParagraphChunking(int textSize)
    {
        var text = GenerateText(textSize);
        var options = new ChunkOptions(
            MaxSize: 500,
            Overlap: 50);
        await _paragraphStrategy.ChunkAsync(text, options);
    }

    [Benchmark]
    public async Task MarkdownAwareChunking()
    {
        var options = new ChunkOptions(
            MaxSize: 500,
            Overlap: 50);
        await _markdownStrategy.ChunkAsync(_markdownText, options);
    }

    private static string GenerateText(int length)
    {
        var words = new[] { "the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog", "and", "runs", "fast" };
        var random = new Random(42);
        var text = new System.Text.StringBuilder(length);
        
        while (text.Length < length)
        {
            text.Append(words[random.Next(words.Length)]);
            text.Append(' ');
            if (random.Next(10) == 0)
            {
                text.Append(". ");
            }
        }
        
        return text.ToString().Substring(0, Math.Min(length, text.Length));
    }

    private static string GenerateMarkdownText(int length)
    {
        var text = GenerateText(length);
        var lines = text.Split('.');
        var markdown = new System.Text.StringBuilder();
        
        for (int i = 0; i < lines.Length; i++)
        {
            if (i % 5 == 0)
            {
                markdown.AppendLine($"## Heading {i / 5 + 1}");
            }
            markdown.AppendLine(lines[i].Trim());
        }
        
        return markdown.ToString();
    }
}
