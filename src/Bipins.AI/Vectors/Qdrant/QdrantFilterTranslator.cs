using System.Text.Json;
using Bipins.AI.Vector;

namespace Bipins.AI.Vectors.Qdrant;

/// <summary>
/// Translates VectorFilter to Qdrant filter format.
/// </summary>
public static class QdrantFilterTranslator
{
    /// <summary>
    /// Translates a VectorFilter to Qdrant filter JSON.
    /// </summary>
    public static JsonElement Translate(VectorFilter filter)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(TranslateInternal(filter));
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static Dictionary<string, object> TranslateInternal(VectorFilter filter)
    {
        return filter switch
        {
            VectorFilterAnd and => new Dictionary<string, object>
            {
                ["must"] = and.Filters.Select(TranslateInternal).ToArray()
            },
            VectorFilterOr or => new Dictionary<string, object>
            {
                ["should"] = or.Filters.Select(TranslateInternal).ToArray()
            },
            VectorFilterNot not => new Dictionary<string, object>
            {
                ["must_not"] = new[] { TranslateInternal(not.Filter) }
            },
            VectorFilterPredicate pred => TranslatePredicate(pred.PredicateValue),
            _ => throw new ArgumentException($"Unknown filter type: {filter.GetType()}")
        };
    }

    private static Dictionary<string, object> TranslatePredicate(FilterPredicate predicate)
    {
        return predicate.Operator switch
        {
            FilterOperator.Eq => new Dictionary<string, object>
            {
                ["key"] = predicate.Field,
                ["match"] = new Dictionary<string, object> { ["value"] = predicate.Value }
            },
            FilterOperator.Ne => new Dictionary<string, object>
            {
                ["key"] = predicate.Field,
                ["match"] = new Dictionary<string, object> { ["except"] = new[] { predicate.Value } }
            },
            FilterOperator.Gt => new Dictionary<string, object>
            {
                ["key"] = predicate.Field,
                ["range"] = new Dictionary<string, object> { ["gt"] = predicate.Value }
            },
            FilterOperator.Gte => new Dictionary<string, object>
            {
                ["key"] = predicate.Field,
                ["range"] = new Dictionary<string, object> { ["gte"] = predicate.Value }
            },
            FilterOperator.Lt => new Dictionary<string, object>
            {
                ["key"] = predicate.Field,
                ["range"] = new Dictionary<string, object> { ["lt"] = predicate.Value }
            },
            FilterOperator.Lte => new Dictionary<string, object>
            {
                ["key"] = predicate.Field,
                ["range"] = new Dictionary<string, object> { ["lte"] = predicate.Value }
            },
            FilterOperator.Contains => new Dictionary<string, object>
            {
                ["key"] = predicate.Field,
                ["match"] = new Dictionary<string, object> { ["text"] = predicate.Value.ToString() ?? string.Empty }
            },
            FilterOperator.Range => throw new NotSupportedException("Range operator requires two values; use Gte and Lte separately"),
            _ => throw new ArgumentException($"Unsupported operator: {predicate.Operator}")
        };
    }
}

