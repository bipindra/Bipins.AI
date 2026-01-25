using System.Text.RegularExpressions;
using Bipins.AI.Core.Ingestion;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Ingestion.Strategies;

/// <summary>
/// Sentence-aware chunking strategy.
/// </summary>
public class SentenceAwareChunkingStrategy : IChunkingStrategy
{
    private readonly ILogger<SentenceAwareChunkingStrategy> _logger;
    private static readonly Regex SentenceRegex = new(@"[.!?]+\s+", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="SentenceAwareChunkingStrategy"/> class.
    /// </summary>
    public SentenceAwareChunkingStrategy(ILogger<SentenceAwareChunkingStrategy> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public ChunkStrategy StrategyType => ChunkStrategy.Sentence;

    /// <inheritdoc />
    public Task<IReadOnlyList<Chunk>> ChunkAsync(string text, ChunkOptions options, CancellationToken cancellationToken = default)
    {
        var chunks = new List<Chunk>();
        var sentences = SentenceRegex.Split(text).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        
        if (sentences.Count == 0)
        {
            // No sentences found, fall back to fixed-size
            return Task.FromResult<IReadOnlyList<Chunk>>(new List<Chunk>());
        }

        var currentChunk = new List<string>();
        var currentLength = 0;
        var chunkIndex = 0;
        var currentStartIndex = 0;

        foreach (var sentence in sentences)
        {
            var sentenceLength = sentence.Length;
            
            if (currentLength + sentenceLength > options.MaxSize && currentChunk.Count > 0)
            {
                // Create chunk from current sentences
                var chunkText = string.Join(" ", currentChunk);
                var endIndex = currentStartIndex + chunkText.Length;
                var chunk = CreateChunk(chunkIndex++, chunkText, currentStartIndex, endIndex, options);
                chunks.Add(chunk);

                // Start new chunk with overlap
                var overlapText = string.Join(" ", currentChunk.TakeLast(2)); // Last 2 sentences for overlap
                currentChunk = new List<string> { overlapText, sentence };
                currentLength = overlapText.Length + sentenceLength;
                currentStartIndex = endIndex - overlapText.Length;
            }
            else
            {
                currentChunk.Add(sentence);
                currentLength += sentenceLength + 1; // +1 for space
            }
        }

        // Add remaining sentences
        if (currentChunk.Count > 0)
        {
            var chunkText = string.Join(" ", currentChunk);
            var endIndex = currentStartIndex + chunkText.Length;
            var chunk = CreateChunk(chunkIndex++, chunkText, currentStartIndex, endIndex, options);
            chunks.Add(chunk);
        }

        _logger.LogInformation("Created {Count} chunks using sentence-aware strategy", chunks.Count);
        return Task.FromResult<IReadOnlyList<Chunk>>(chunks);
    }

    private static Chunk CreateChunk(int index, string text, int startIndex, int endIndex, ChunkOptions options)
    {
        var metadata = new Dictionary<string, object>
        {
            ["chunkIndex"] = index,
            ["strategy"] = "Sentence"
        };

        return new Chunk(
            $"chunk_{index}_{Guid.NewGuid():N}",
            text.Trim(),
            startIndex,
            endIndex,
            metadata);
    }
}
