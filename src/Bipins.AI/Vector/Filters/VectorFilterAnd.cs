namespace Bipins.AI.Core.Vector;

/// <summary>
/// Logical AND filter (all conditions must match).
/// </summary>
public record VectorFilterAnd(IReadOnlyList<VectorFilter> Filters) : VectorFilter;
