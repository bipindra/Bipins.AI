using System.Text.Json;
using Bipins.AI.Connectors.Vector.Qdrant;
using Bipins.AI.Core.Vector;
using Xunit;

namespace Bipins.AI.UnitTests;

public class FilterTranslationTests
{
    [Fact]
    public void Translate_SimplePredicate_ProducesCorrectQdrantFilter()
    {
        var filter = new VectorFilterPredicate(
            new FilterPredicate("field1", FilterOperator.Eq, "value1"));

        var result = QdrantFilterTranslator.Translate(filter);

        Assert.True(result.ValueKind == JsonValueKind.Object);
        var json = result.GetRawText();
        Assert.Contains("field1", json);
        Assert.Contains("value1", json);
    }

    [Fact]
    public void Translate_AndFilter_ProducesMustClause()
    {
        var filter = new VectorFilterAnd(new[]
        {
            new VectorFilterPredicate(new FilterPredicate("field1", FilterOperator.Eq, "value1")),
            new VectorFilterPredicate(new FilterPredicate("field2", FilterOperator.Gt, 10))
        });

        var result = QdrantFilterTranslator.Translate(filter);

        var json = result.GetRawText();
        Assert.Contains("must", json);
    }

    [Fact]
    public void Translate_OrFilter_ProducesShouldClause()
    {
        var filter = new VectorFilterOr(new[]
        {
            new VectorFilterPredicate(new FilterPredicate("field1", FilterOperator.Eq, "value1")),
            new VectorFilterPredicate(new FilterPredicate("field2", FilterOperator.Eq, "value2"))
        });

        var result = QdrantFilterTranslator.Translate(filter);

        var json = result.GetRawText();
        Assert.Contains("should", json);
    }

    [Fact]
    public void Translate_NotFilter_ProducesMustNotClause()
    {
        var filter = new VectorFilterNot(
            new VectorFilterPredicate(new FilterPredicate("field1", FilterOperator.Eq, "value1")));

        var result = QdrantFilterTranslator.Translate(filter);

        var json = result.GetRawText();
        Assert.Contains("must_not", json);
    }
}
