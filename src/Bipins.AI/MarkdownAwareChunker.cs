using Bipins.AI.Core.Ingestion;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Ingestion;

/// <summary>
/// Chunker that delegates to chunking strategies based on ChunkOptions.
/// </summary>
public class MarkdownAwareChunker : IChunker
{
    private readonly ILogger<MarkdownAwareChunker> _logger;
    private readonly IChunkingStrategyFactory _strategyFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownAwareChunker"/> class.
    /// </summary>
    public MarkdownAwareChunker(
        ILogger<MarkdownAwareChunker> logger,
        IChunkingStrategyFactory strategyFactory)
    {
        _logger = logger;
        _strategyFactory = strategyFactory;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Chunk>> ChunkAsync(string text, ChunkOptions options, CancellationToken cancellationToken = default)
    {
        var strategy = _strategyFactory.GetStrategy(options.Strategy);
        return strategy.ChunkAsync(text, options, cancellationToken);
    }
}
