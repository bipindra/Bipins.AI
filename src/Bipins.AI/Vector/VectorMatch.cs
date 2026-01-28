namespace Bipins.AI.Vector;

/// <summary>
/// A matching vector record with its score.
/// </summary>
/// <param name="Record">The vector record.</param>
/// <param name="Score">The similarity score (higher is more similar).</param>
public record VectorMatch(
    VectorRecord Record,
    float Score);
