namespace Bipins.AI.Core.Models;

/// <summary>
/// Request for text embedding.
/// </summary>
/// <param name="Inputs">List of text inputs to embed.</param>
/// <param name="ModelId">The embedding model identifier.</param>
/// <param name="Metadata">Additional metadata.</param>
public record EmbeddingRequest(
    IReadOnlyList<string> Inputs,
    string? ModelId = null,
    Dictionary<string, object>? Metadata = null);
