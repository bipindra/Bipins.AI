using Bipins.AI.Core.Ingestion;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Ingestion.Strategies;

/// <summary>
/// Fixed-size chunking strategy.
/// </summary>
public class FixedSizeChunkingStrategy : IChunkingStrategy
{
    private readonly ILogger<FixedSizeChunkingStrategy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedSizeChunkingStrategy"/> class.
    /// </summary>
    public FixedSizeChunkingStrategy(ILogger<FixedSizeChunkingStrategy> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public ChunkStrategy StrategyType => ChunkStrategy.FixedSize;

    /// <inheritdoc />
    public Task<IReadOnlyList<Chunk>> ChunkAsync(string text, ChunkOptions options, CancellationToken cancellationToken = default)
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
            var chunk = CreateChunk(chunkIndex++, chunkText, startIndex, endIndex, options);
            chunks.Add(chunk);

            // Move start index with overlap
            startIndex = Math.Max(startIndex + 1, endIndex - options.Overlap);
        }

        _logger.LogInformation("Created {Count} chunks using fixed-size strategy", chunks.Count);
        return Task.FromResult<IReadOnlyList<Chunk>>(chunks);
    }

    private static Chunk CreateChunk(int index, string text, int startIndex, int endIndex, ChunkOptions options)
    {
        var metadata = new Dictionary<string, object>
        {
            ["chunkIndex"] = index,
            ["strategy"] = "FixedSize"
        };

        return new Chunk(
            $"chunk_{index}_{Guid.NewGuid():N}",
            text.Trim(),
            startIndex,
            endIndex,
            metadata);
    }
}
