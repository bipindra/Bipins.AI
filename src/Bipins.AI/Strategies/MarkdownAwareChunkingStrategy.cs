using System.Text;
using System.Text.RegularExpressions;
using Bipins.AI.Core.Ingestion;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Ingestion.Strategies;

/// <summary>
/// Markdown-aware chunking strategy.
/// </summary>
public class MarkdownAwareChunkingStrategy : IChunkingStrategy
{
    private readonly ILogger<MarkdownAwareChunkingStrategy> _logger;
    private static readonly Regex HeadingRegex = new(@"^(#{1,6})\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownAwareChunkingStrategy"/> class.
    /// </summary>
    public MarkdownAwareChunkingStrategy(ILogger<MarkdownAwareChunkingStrategy> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public ChunkStrategy StrategyType => ChunkStrategy.MarkdownAware;

    /// <inheritdoc />
    public Task<IReadOnlyList<Chunk>> ChunkAsync(string text, ChunkOptions options, CancellationToken cancellationToken = default)
    {
        var chunks = new List<Chunk>();
        var headingMatches = HeadingRegex.Matches(text).Cast<Match>().ToList();

        if (headingMatches.Count == 0)
        {
            // No headings found, fall back to fixed-size chunking
            _logger.LogDebug("No markdown headings found, falling back to fixed-size chunking");
            return Task.FromResult<IReadOnlyList<Chunk>>(new List<Chunk>());
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
        return Task.FromResult<IReadOnlyList<Chunk>>(chunks);
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
            ["chunkIndex"] = index,
            ["strategy"] = "MarkdownAware"
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
