using System.Text.Json;
using Bipins.AI.Core.Vector;

namespace Bipins.AI.Vectors.Weaviate;

/// <summary>
/// Translates VectorFilter to Weaviate where filter format.
/// </summary>
public static class WeaviateFilterTranslator
{
    /// <summary>
    /// Translates a VectorFilter to Weaviate where clause.
    /// </summary>
    public static Dictionary<string, object>? Translate(VectorFilter? filter)
    {
        if (filter == null)
        {
            return null;
        }

        return TranslateInternal(filter);
    }

    private static Dictionary<string, object> TranslateInternal(VectorFilter filter)
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

    private static Dictionary<string, object> TranslateAnd(VectorFilterAnd and)
    {
        var operands = and.Filters.Select(TranslateInternal).ToList();
        if (operands.Count == 1)
        {
            return operands[0];
        }
        return new Dictionary<string, object>
        {
            ["operator"] = "And",
            ["operands"] = operands
        };
    }

    private static Dictionary<string, object> TranslateOr(VectorFilterOr or)
    {
        var operands = or.Filters.Select(TranslateInternal).ToList();
        if (operands.Count == 1)
        {
            return operands[0];
        }
        return new Dictionary<string, object>
        {
            ["operator"] = "Or",
            ["operands"] = operands
        };
    }

    private static Dictionary<string, object> TranslateNot(VectorFilterNot not)
    {
        return new Dictionary<string, object>
        {
            ["operator"] = "Not",
            ["operands"] = new[] { TranslateInternal(not.Filter) }
        };
    }

    private static Dictionary<string, object> TranslatePredicate(FilterPredicate predicate)
    {
        var path = new[] { predicate.Field };
        var value = predicate.Value;

        return predicate.Operator switch
        {
            FilterOperator.Eq => new Dictionary<string, object>
            {
                ["path"] = path,
                ["operator"] = "Equal",
                ["valueText"] = value.ToString() ?? string.Empty
            },
            FilterOperator.Ne => new Dictionary<string, object>
            {
                ["path"] = path,
                ["operator"] = "NotEqual",
                ["valueText"] = value.ToString() ?? string.Empty
            },
            FilterOperator.Gt => new Dictionary<string, object>
            {
                ["path"] = path,
                ["operator"] = "GreaterThan",
                ["valueNumber"] = Convert.ToDouble(value)
            },
            FilterOperator.Gte => new Dictionary<string, object>
            {
                ["path"] = path,
                ["operator"] = "GreaterThanEqual",
                ["valueNumber"] = Convert.ToDouble(value)
            },
            FilterOperator.Lt => new Dictionary<string, object>
            {
                ["path"] = path,
                ["operator"] = "LessThan",
                ["valueNumber"] = Convert.ToDouble(value)
            },
            FilterOperator.Lte => new Dictionary<string, object>
            {
                ["path"] = path,
                ["operator"] = "LessThanEqual",
                ["valueNumber"] = Convert.ToDouble(value)
            },
            FilterOperator.Contains => new Dictionary<string, object>
            {
                ["path"] = path,
                ["operator"] = "Like",
                ["valueText"] = $"*{value}*"
            },
            FilterOperator.Range => throw new NotSupportedException("Range operator requires two values; use Gte and Lte separately"),
            _ => throw new ArgumentException($"Unsupported operator: {predicate.Operator}")
        };
    }
}

