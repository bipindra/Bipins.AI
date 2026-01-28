namespace Bipins.AI.Vector;

/// <summary>
/// Logical OR filter (any condition must match).
/// </summary>
public record VectorFilterOr(IReadOnlyList<VectorFilter> Filters) : VectorFilter;
