namespace Bipins.AI.Vector;

/// <summary>
/// A predicate in a filter expression.
/// </summary>
/// <param name="Field">The field name to filter on.</param>
/// <param name="Operator">The comparison operator.</param>
/// <param name="Value">The value to compare against.</param>
public record FilterPredicate(
    string Field,
    FilterOperator Operator,
    object Value);
