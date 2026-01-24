namespace Bipins.AI.Core.Vector;

/// <summary>
/// Logical NOT filter (condition must not match).
/// </summary>
public record VectorFilterNot(VectorFilter Filter) : VectorFilter;
