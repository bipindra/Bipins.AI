using System.Text.RegularExpressions;
using Bipins.AI.Core.Ingestion;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Ingestion.Strategies;

/// <summary>
/// Paragraph-aware chunking strategy.
/// </summary>
public class ParagraphChunkingStrategy : IChunkingStrategy
{
    private readonly ILogger<ParagraphChunkingStrategy> _logger;
    private static readonly Regex ParagraphRegex = new(@"\n\s*\n", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Initializes a new instance of the <see cref="ParagraphChunkingStrategy"/> class.
    /// </summary>
    public ParagraphChunkingStrategy(ILogger<ParagraphChunkingStrategy> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public ChunkStrategy StrategyType => ChunkStrategy.Paragraph;

    /// <inheritdoc />
    public Task<IReadOnlyList<Chunk>> ChunkAsync(string text, ChunkOptions options, CancellationToken cancellationToken = default)
    {
        var chunks = new List<Chunk>();
        var paragraphs = ParagraphRegex.Split(text).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();

        if (paragraphs.Count == 0)
        {
            // No paragraphs found, treat entire text as one paragraph
            paragraphs = new List<string> { text };
        }

        var currentChunk = new List<string>();
        var currentLength = 0;
        var chunkIndex = 0;
        var currentStartIndex = 0;

        foreach (var paragraph in paragraphs)
        {
            var paragraphLength = paragraph.Length;

            if (currentLength + paragraphLength > options.MaxSize && currentChunk.Count > 0)
            {
                // Create chunk from current paragraphs
                var chunkText = string.Join("\n\n", currentChunk);
                var endIndex = currentStartIndex + chunkText.Length;
                var chunk = CreateChunk(chunkIndex++, chunkText, currentStartIndex, endIndex, options);
                chunks.Add(chunk);

                // Start new chunk with overlap (last paragraph)
                currentChunk = new List<string> { currentChunk.Last(), paragraph };
                currentLength = currentChunk[0].Length + paragraphLength;
                currentStartIndex = endIndex - currentChunk[0].Length;
            }
            else
            {
                currentChunk.Add(paragraph);
                currentLength += paragraphLength + 2; // +2 for paragraph separator
            }
        }

        // Add remaining paragraphs
        if (currentChunk.Count > 0)
        {
            var chunkText = string.Join("\n\n", currentChunk);
            var endIndex = currentStartIndex + chunkText.Length;
            var chunk = CreateChunk(chunkIndex++, chunkText, currentStartIndex, endIndex, options);
            chunks.Add(chunk);
        }

        _logger.LogInformation("Created {Count} chunks using paragraph-aware strategy", chunks.Count);
        return Task.FromResult<IReadOnlyList<Chunk>>(chunks);
    }

    private static Chunk CreateChunk(int index, string text, int startIndex, int endIndex, ChunkOptions options)
    {
        var metadata = new Dictionary<string, object>
        {
            ["chunkIndex"] = index,
            ["strategy"] = "Paragraph"
        };

        return new Chunk(
            $"chunk_{index}_{Guid.NewGuid():N}",
            text.Trim(),
            startIndex,
            endIndex,
            metadata);
    }
}
