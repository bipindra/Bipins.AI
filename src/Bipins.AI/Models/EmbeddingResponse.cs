namespace Bipins.AI.Core.Models;

/// <summary>
/// Response from an embedding request.
/// </summary>
/// <param name="Vectors">List of embedding vectors (one per input).</param>
/// <param name="Usage">Token usage information.</param>
/// <param name="ModelId">The model identifier used.</param>
public record EmbeddingResponse(
    IReadOnlyList<ReadOnlyMemory<float>> Vectors,
    Usage? Usage = null,
    string? ModelId = null);
