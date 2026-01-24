namespace Bipins.AI.Core.Vector;

/// <summary>
/// Predicate filter (field comparison).
/// </summary>
public record VectorFilterPredicate(FilterPredicate PredicateValue) : VectorFilter;
