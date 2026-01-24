namespace Bipins.AI.Core.Vector;

/// <summary>
/// Comparison operators for filter predicates.
/// </summary>
public enum FilterOperator
{
    /// <summary>
    /// Equals.
    /// </summary>
    Eq,

    /// <summary>
    /// Not equals.
    /// </summary>
    Ne,

    /// <summary>
    /// Greater than.
    /// </summary>
    Gt,

    /// <summary>
    /// Greater than or equal.
    /// </summary>
    Gte,

    /// <summary>
    /// Less than.
    /// </summary>
    Lt,

    /// <summary>
    /// Less than or equal.
    /// </summary>
    Lte,

    /// <summary>
    /// Contains (for strings/arrays).
    /// </summary>
    Contains,

    /// <summary>
    /// Range (value must be between two values).
    /// </summary>
    Range
}
