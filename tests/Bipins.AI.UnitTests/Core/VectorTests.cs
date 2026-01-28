using Bipins.AI.Vector;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class VectorTests
{
    [Fact]
    public void VectorFilterBuilder_Equal_CreatesPredicate()
    {
        var builder = VectorFilterBuilder.Create();
        var filter = builder.Equal("field1", "value1").Build();

        Assert.NotNull(filter);
        var predicate = Assert.IsType<VectorFilterPredicate>(filter);
        Assert.Equal("field1", predicate.PredicateValue.Field);
        Assert.Equal(FilterOperator.Eq, predicate.PredicateValue.Operator);
        Assert.Equal("value1", predicate.PredicateValue.Value);
    }

    [Fact]
    public void VectorFilterBuilder_NotEqual_CreatesPredicate()
    {
        var builder = VectorFilterBuilder.Create();
        var filter = builder.NotEqual("field1", "value1").Build();

        var predicate = Assert.IsType<VectorFilterPredicate>(filter);
        Assert.Equal(FilterOperator.Ne, predicate.PredicateValue.Operator);
    }

    [Fact]
    public void VectorFilterBuilder_GreaterThan_CreatesPredicate()
    {
        var builder = VectorFilterBuilder.Create();
        var filter = builder.GreaterThan("field1", 10).Build();

        var predicate = Assert.IsType<VectorFilterPredicate>(filter);
        Assert.Equal(FilterOperator.Gt, predicate.PredicateValue.Operator);
        Assert.Equal(10, predicate.PredicateValue.Value);
    }

    [Fact]
    public void VectorFilterBuilder_LessThan_CreatesPredicate()
    {
        var builder = VectorFilterBuilder.Create();
        var filter = builder.LessThan("field1", 10).Build();

        var predicate = Assert.IsType<VectorFilterPredicate>(filter);
        Assert.Equal(FilterOperator.Lt, predicate.PredicateValue.Operator);
    }

    [Fact]
    public void VectorFilterBuilder_Contains_CreatesPredicate()
    {
        var builder = VectorFilterBuilder.Create();
        var filter = builder.Contains("field1", "search").Build();

        var predicate = Assert.IsType<VectorFilterPredicate>(filter);
        Assert.Equal(FilterOperator.Contains, predicate.PredicateValue.Operator);
        Assert.Equal("search", predicate.PredicateValue.Value);
    }

    [Fact]
    public void VectorFilterBuilder_Range_CreatesTwoPredicates()
    {
        var builder = VectorFilterBuilder.Create();
        var filter = builder.Range("field1", 10, 20).Build();

        var andFilter = Assert.IsType<VectorFilterAnd>(filter);
        Assert.Equal(2, andFilter.Filters.Count);
    }

    [Fact]
    public void VectorFilterBuilder_And_CombinesFilters()
    {
        var builder = VectorFilterBuilder.Create();
        var filter = builder
            .Equal("field1", "value1")
            .GreaterThan("field2", 10)
            .And();

        var andFilter = Assert.IsType<VectorFilterAnd>(filter);
        Assert.Equal(2, andFilter.Filters.Count);
    }

    [Fact]
    public void VectorFilterBuilder_Or_CombinesFilters()
    {
        var builder = VectorFilterBuilder.Create();
        var filter = builder
            .Equal("field1", "value1")
            .Equal("field2", "value2")
            .Or();

        var orFilter = Assert.IsType<VectorFilterOr>(filter);
        Assert.Equal(2, orFilter.Filters.Count);
    }

    [Fact]
    public void VectorFilterBuilder_Not_NegatesFilter()
    {
        var builder = VectorFilterBuilder.Create();
        var filter = builder.Equal("field1", "value1").Not();

        var notFilter = Assert.IsType<VectorFilterNot>(filter);
        Assert.NotNull(notFilter.Filter);
    }

    [Fact]
    public void VectorFilterBuilder_And_WithSingleFilter_ReturnsFilter()
    {
        var builder = VectorFilterBuilder.Create();
        var filter = builder.Equal("field1", "value1").And();

        Assert.IsType<VectorFilterPredicate>(filter);
    }

    [Fact]
    public void VectorFilterBuilder_And_WithNoFilters_ThrowsException()
    {
        var builder = VectorFilterBuilder.Create();

        Assert.Throws<InvalidOperationException>(() => builder.And());
    }

    [Fact]
    public void VectorFilterBuilder_Or_WithNoFilters_ThrowsException()
    {
        var builder = VectorFilterBuilder.Create();

        Assert.Throws<InvalidOperationException>(() => builder.Or());
    }

    [Fact]
    public void VectorFilterBuilder_Not_WithNoFilters_ThrowsException()
    {
        var builder = VectorFilterBuilder.Create();

        Assert.Throws<InvalidOperationException>(() => builder.Not());
    }

    [Fact]
    public void VectorFilterBuilder_AndGroup_CreatesNestedFilter()
    {
        var builder = VectorFilterBuilder.Create();
        var filter = builder
            .Equal("field1", "value1")
            .AndGroup(g => g
                .Equal("field2", "value2")
                .Equal("field3", "value3"))
            .Build();

        var andFilter = Assert.IsType<VectorFilterAnd>(filter);
        Assert.Equal(2, andFilter.Filters.Count);
    }

    [Fact]
    public void VectorFilterBuilder_OrGroup_CreatesNestedFilter()
    {
        var builder = VectorFilterBuilder.Create();
        var filter = builder
            .Equal("field1", "value1")
            .OrGroup(g => g
                .Equal("field2", "value2")
                .Equal("field3", "value3"))
            .Or();

        var orFilter = Assert.IsType<VectorFilterOr>(filter);
        Assert.Equal(2, orFilter.Filters.Count);
    }

    [Fact]
    public void VectorFilterPredicate_Creation_SetsProperties()
    {
        var predicate = new FilterPredicate("field1", FilterOperator.Eq, "value1");
        var filter = new VectorFilterPredicate(predicate);

        Assert.Equal(predicate, filter.PredicateValue);
    }

    [Fact]
    public void VectorQueryRequest_Creation_SetsProperties()
    {
        var vector = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });
        var request = new VectorQueryRequest(
            QueryVector: vector,
            TopK: 10,
            TenantId: "tenant1",
            Filter: null,
            CollectionName: "collection1");

        Assert.Equal(vector, request.QueryVector);
        Assert.Equal(10, request.TopK);
        Assert.Equal("tenant1", request.TenantId);
        Assert.Null(request.Filter);
        Assert.Equal("collection1", request.CollectionName);
    }

    [Fact]
    public void VectorQueryRequest_WithFilter_SetsFilter()
    {
        var vector = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f });
        var filter = new VectorFilterPredicate(new FilterPredicate("field1", FilterOperator.Eq, "value1"));
        var request = new VectorQueryRequest(vector, 5, "tenant1", filter);

        Assert.NotNull(request.Filter);
        Assert.Equal(filter, request.Filter);
    }

    [Fact]
    public void VectorRecord_Creation_SetsProperties()
    {
        var vector = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f });
        var metadata = new Dictionary<string, object> { { "key", "value" } };
        var record = new VectorRecord(
            Id: "id1",
            Vector: vector,
            Text: "text1",
            Metadata: metadata,
            SourceUri: "uri1",
            DocId: "doc1",
            ChunkId: "chunk1",
            TenantId: "tenant1",
            VersionId: "v1");

        Assert.Equal("id1", record.Id);
        Assert.Equal(vector, record.Vector);
        Assert.Equal("text1", record.Text);
        Assert.Equal(metadata, record.Metadata);
        Assert.Equal("uri1", record.SourceUri);
        Assert.Equal("doc1", record.DocId);
        Assert.Equal("chunk1", record.ChunkId);
        Assert.Equal("tenant1", record.TenantId);
        Assert.Equal("v1", record.VersionId);
    }

    [Fact]
    public void VectorRecord_WithMinimalProperties_CreatesRecord()
    {
        var vector = new ReadOnlyMemory<float>(new float[] { 0.1f });
        var record = new VectorRecord("id1", vector, "text1");

        Assert.Equal("id1", record.Id);
        Assert.Equal("text1", record.Text);
        Assert.Null(record.Metadata);
        Assert.Null(record.TenantId);
    }
}
