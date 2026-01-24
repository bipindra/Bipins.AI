using System.Text;
using System.Text.RegularExpressions;
using Bipins.AI.Core.Ingestion;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Ingestion;

/// <summary>
/// Chunks text with awareness of markdown headings.
/// </summary>
public class MarkdownAwareChunker : IChunker
{
    private readonly ILogger<MarkdownAwareChunker> _logger;
    private static readonly Regex HeadingRegex = new(@"^(#{1,6})\s+(.+)$", RegexOptions.Multiline);

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownAwareChunker"/> class.
    /// </summary>
    public MarkdownAwareChunker(ILogger<MarkdownAwareChunker> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Chunk>> ChunkAsync(string text, ChunkOptions options, CancellationToken cancellationToken = default)
    {
        if (options.Strategy == ChunkStrategy.MarkdownAware)
        {
            return Task.FromResult(ChunkByMarkdown(text, options));
        }

        return Task.FromResult(ChunkByFixedSize(text, options));
    }

    private IReadOnlyList<Chunk> ChunkByMarkdown(string text, ChunkOptions options)
    {
        var chunks = new List<Chunk>();
        var headingMatches = HeadingRegex.Matches(text).Cast<Match>().ToList();

        if (headingMatches.Count == 0)
        {
            // No headings found, fall back to fixed-size chunking
            return ChunkByFixedSize(text, options);
        }

        var currentSection = new StringBuilder();
        var currentHeading = "Document";
        var currentStartIndex = 0;
        var chunkIndex = 0;

        for (int i = 0; i < headingMatches.Count; i++)
        {
            var match = headingMatches[i];
            var nextStart = i + 1 < headingMatches.Count ? headingMatches[i + 1].Index : text.Length;
            var sectionText = text.Substring(match.Index, nextStart - match.Index);

            // If current section + new section exceeds max size, create a chunk
            if (currentSection.Length + sectionText.Length > options.MaxSize && currentSection.Length > 0)
            {
                var chunk = CreateChunk(
                    chunkIndex++,
                    currentSection.ToString(),
                    currentStartIndex,
                    currentStartIndex + currentSection.Length,
                    currentHeading,
                    options);

                chunks.Add(chunk);
                currentSection.Clear();
                currentStartIndex = match.Index;
            }

            currentHeading = match.Groups[2].Value.Trim();
            currentSection.Append(sectionText);
        }

        // Add remaining content
        if (currentSection.Length > 0)
        {
            var chunk = CreateChunk(
                chunkIndex++,
                currentSection.ToString(),
                currentStartIndex,
                currentStartIndex + currentSection.Length,
                currentHeading,
                options);

            chunks.Add(chunk);
        }

        _logger.LogInformation("Created {Count} chunks using markdown-aware strategy", chunks.Count);
        return chunks;
    }

    private IReadOnlyList<Chunk> ChunkByFixedSize(string text, ChunkOptions options)
    {
        var chunks = new List<Chunk>();
        var startIndex = 0;
        var chunkIndex = 0;

        while (startIndex < text.Length)
        {
            var endIndex = Math.Min(startIndex + options.MaxSize, text.Length);

            // Try to break at a sentence or word boundary
            if (endIndex < text.Length)
            {
                var lastPeriod = text.LastIndexOf('.', endIndex - 1, Math.Min(100, endIndex - startIndex));
                var lastSpace = text.LastIndexOf(' ', endIndex - 1, Math.Min(50, endIndex - startIndex));

                if (lastPeriod > startIndex)
                {
                    endIndex = lastPeriod + 1;
                }
                else if (lastSpace > startIndex)
                {
                    endIndex = lastSpace + 1;
                }
            }

            var chunkText = text.Substring(startIndex, endIndex - startIndex);
            var chunk = CreateChunk(chunkIndex++, chunkText, startIndex, endIndex, null, options);
            chunks.Add(chunk);

            // Move start index with overlap
            startIndex = Math.Max(startIndex + 1, endIndex - options.Overlap);
        }

        _logger.LogInformation("Created {Count} chunks using fixed-size strategy", chunks.Count);
        return chunks;
    }

    private static Chunk CreateChunk(
        int index,
        string text,
        int startIndex,
        int endIndex,
        string? heading,
        ChunkOptions options)
    {
        var metadata = new Dictionary<string, object>
        {
            ["chunkIndex"] = index
        };

        if (heading != null)
        {
            metadata["heading"] = heading;
        }

        return new Chunk(
            $"chunk_{index}_{Guid.NewGuid():N}",
            text.Trim(),
            startIndex,
            endIndex,
            metadata);
    }
}
