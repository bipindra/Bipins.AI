using Bipins.AI.Vector;

namespace Bipins.AI.Vectors.Milvus;

/// <summary>
/// Translates VectorFilter to Milvus expression format.
/// </summary>
public static class MilvusFilterTranslator
{
    /// <summary>
    /// Translates a VectorFilter to Milvus expression string.
    /// </summary>
    public static string? Translate(VectorFilter? filter)
    {
        if (filter == null)
        {
            return null;
        }

        return TranslateInternal(filter);
    }

    private static string TranslateInternal(VectorFilter filter)
    {
        return filter switch
        {
            VectorFilterAnd and => TranslateAnd(and),
            VectorFilterOr or => TranslateOr(or),
            VectorFilterNot not => TranslateNot(not),
            VectorFilterPredicate pred => TranslatePredicate(pred.PredicateValue),
            _ => throw new ArgumentException($"Unknown filter type: {filter.GetType()}")
        };
    }

    private static string TranslateAnd(VectorFilterAnd and)
    {
        var conditions = and.Filters.Select(TranslateInternal).ToList();
        if (conditions.Count == 1)
        {
            return conditions[0];
        }
        return $"({string.Join(" && ", conditions)})";
    }

    private static string TranslateOr(VectorFilterOr or)
    {
        var conditions = or.Filters.Select(TranslateInternal).ToList();
        if (conditions.Count == 1)
        {
            return conditions[0];
        }
        return $"({string.Join(" || ", conditions)})";
    }

    private static string TranslateNot(VectorFilterNot not)
    {
        return $"!({TranslateInternal(not.Filter)})";
    }

    private static string TranslatePredicate(FilterPredicate predicate)
    {
        var field = predicate.Field;
        var value = predicate.Value;

        return predicate.Operator switch
        {
            FilterOperator.Eq => $"{field} == \"{value}\"",
            FilterOperator.Ne => $"{field} != \"{value}\"",
            FilterOperator.Gt => $"{field} > {value}",
            FilterOperator.Gte => $"{field} >= {value}",
            FilterOperator.Lt => $"{field} < {value}",
            FilterOperator.Lte => $"{field} <= {value}",
            FilterOperator.Contains => $"{field}.contains(\"{value}\")",
            FilterOperator.Range => throw new NotSupportedException("Range operator requires two values; use Gte and Lte separately"),
            _ => throw new ArgumentException($"Unsupported operator: {predicate.Operator}")
        };
    }
}

