using Bipins.AI.Vector;

namespace Bipins.AI.Vectors.Pinecone;

/// <summary>
/// Translates VectorFilter to Pinecone filter format.
/// </summary>
public static class PineconeFilterTranslator
{
    /// <summary>
    /// Translates a VectorFilter to Pinecone filter dictionary.
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
        var conditions = and.Filters.Select(TranslateInternal).ToList();
        if (conditions.Count == 1)
        {
            return conditions[0];
        }
        // Pinecone uses $and for multiple conditions
        return new Dictionary<string, object> { ["$and"] = conditions };
    }

    private static Dictionary<string, object> TranslateOr(VectorFilterOr or)
    {
        var conditions = or.Filters.Select(TranslateInternal).ToList();
        if (conditions.Count == 1)
        {
            return conditions[0];
        }
        return new Dictionary<string, object> { ["$or"] = conditions };
    }

    private static Dictionary<string, object> TranslateNot(VectorFilterNot not)
    {
        return new Dictionary<string, object> { ["$not"] = TranslateInternal(not.Filter) };
    }

    private static Dictionary<string, object> TranslatePredicate(FilterPredicate predicate)
    {
        return predicate.Operator switch
        {
            FilterOperator.Eq => new Dictionary<string, object> { [predicate.Field] = predicate.Value },
            FilterOperator.Ne => new Dictionary<string, object> { [predicate.Field] = new Dictionary<string, object> { ["$ne"] = predicate.Value } },
            FilterOperator.Gt => new Dictionary<string, object> { [predicate.Field] = new Dictionary<string, object> { ["$gt"] = predicate.Value } },
            FilterOperator.Gte => new Dictionary<string, object> { [predicate.Field] = new Dictionary<string, object> { ["$gte"] = predicate.Value } },
            FilterOperator.Lt => new Dictionary<string, object> { [predicate.Field] = new Dictionary<string, object> { ["$lt"] = predicate.Value } },
            FilterOperator.Lte => new Dictionary<string, object> { [predicate.Field] = new Dictionary<string, object> { ["$lte"] = predicate.Value } },
            FilterOperator.Contains => new Dictionary<string, object> { [predicate.Field] = new Dictionary<string, object> { ["$regex"] = predicate.Value.ToString() ?? string.Empty } },
            FilterOperator.Range => throw new NotSupportedException("Range operator requires two values; use Gte and Lte separately"),
            _ => throw new ArgumentException($"Unsupported operator: {predicate.Operator}")
        };
    }
}

