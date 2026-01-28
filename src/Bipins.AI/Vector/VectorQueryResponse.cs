namespace Bipins.AI.Vector;

/// <summary>
/// Response from a vector query.
/// </summary>
/// <param name="Matches">List of matching records with scores.</param>
public record VectorQueryResponse(
    IReadOnlyList<VectorMatch> Matches);
