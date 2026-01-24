using Bipins.AI.Core.Vector;
using Xunit;

namespace Bipins.AI.UnitTests.Vector;

public class VectorFilterBuilderTests
{
    [Fact]
    public void Create_ReturnsNewBuilder()
    {
        var builder = VectorFilterBuilder.Create();
        Assert.NotNull(builder);
    }

    [Fact]
    public void Equal_CreatesPredicateFilter()
    {
        var filter = VectorFilterBuilder.Create()
            .Equal("status", "active")
            .Build();

        Assert.IsType<VectorFilterPredicate>(filter);
        var predicate = (VectorFilterPredicate)filter;
        Assert.Equal("status", predicate.PredicateValue.Field);
        Assert.Equal(FilterOperator.Eq, predicate.PredicateValue.Operator);
        Assert.Equal("active", predicate.PredicateValue.Value);
    }

    [Fact]
    public void NotEqual_CreatesPredicateFilter()
    {
        var filter = VectorFilterBuilder.Create()
            .NotEqual("status", "inactive")
            .Build();

        Assert.IsType<VectorFilterPredicate>(filter);
        var predicate = (VectorFilterPredicate)filter;
        Assert.Equal(FilterOperator.Ne, predicate.PredicateValue.Operator);
    }

    [Fact]
    public void GreaterThan_CreatesPredicateFilter()
    {
        var filter = VectorFilterBuilder.Create()
            .GreaterThan("score", 100)
            .Build();

        Assert.IsType<VectorFilterPredicate>(filter);
        var predicate = (VectorFilterPredicate)filter;
        Assert.Equal(FilterOperator.Gt, predicate.PredicateValue.Operator);
        Assert.Equal(100, predicate.PredicateValue.Value);
    }

    [Fact]
    public void LessThan_CreatesPredicateFilter()
    {
        var filter = VectorFilterBuilder.Create()
            .LessThan("score", 50)
            .Build();

        Assert.IsType<VectorFilterPredicate>(filter);
        var predicate = (VectorFilterPredicate)filter;
        Assert.Equal(FilterOperator.Lt, predicate.PredicateValue.Operator);
    }

    [Fact]
    public void Range_CreatesMultiplePredicates()
    {
        var filter = VectorFilterBuilder.Create()
            .Range("score", 10, 100)
            .Build();

        // Range creates an AND filter with two predicates
        Assert.IsType<VectorFilterAnd>(filter);
        var andFilter = (VectorFilterAnd)filter;
        Assert.Equal(2, andFilter.Filters.Count);
    }

    [Fact]
    public void And_CombinesFiltersWithAnd()
    {
        var filter = VectorFilterBuilder.Create()
            .Equal("status", "active")
            .Equal("type", "document")
            .And();

        Assert.IsType<VectorFilterAnd>(filter);
        var andFilter = (VectorFilterAnd)filter;
        Assert.Equal(2, andFilter.Filters.Count);
    }

    [Fact]
    public void Or_CombinesFiltersWithOr()
    {
        var filter = VectorFilterBuilder.Create()
            .Equal("status", "active")
            .Equal("status", "pending")
            .Or();

        Assert.IsType<VectorFilterOr>(filter);
        var orFilter = (VectorFilterOr)filter;
        Assert.Equal(2, orFilter.Filters.Count);
    }

    [Fact]
    public void Not_NegatesFilter()
    {
        var filter = VectorFilterBuilder.Create()
            .Equal("status", "inactive")
            .Not();

        Assert.IsType<VectorFilterNot>(filter);
        var notFilter = (VectorFilterNot)filter;
        Assert.NotNull(notFilter.Filter);
    }

    [Fact]
    public void AndGroup_CreatesNestedAndFilter()
    {
        var filter = VectorFilterBuilder.Create()
            .Equal("tenantId", "tenant1")
            .AndGroup(group => group
                .Equal("status", "active")
                .Equal("type", "document"))
            .Build();

        Assert.IsType<VectorFilterAnd>(filter);
        var andFilter = (VectorFilterAnd)filter;
        Assert.Equal(2, andFilter.Filters.Count);
    }

    [Fact]
    public void OrGroup_CreatesNestedOrFilter()
    {
        var filter = VectorFilterBuilder.Create()
            .Equal("tenantId", "tenant1")
            .OrGroup(group => group
                .Equal("status", "active")
                .Equal("status", "pending"))
            .Build();

        Assert.IsType<VectorFilterAnd>(filter);
        var andFilter = (VectorFilterAnd)filter;
        Assert.Equal(2, andFilter.Filters.Count);
        // The second filter should be an OR filter
        Assert.IsType<VectorFilterOr>(andFilter.Filters[1]);
    }

    [Fact]
    public void Contains_CreatesTextSearchFilter()
    {
        var filter = VectorFilterBuilder.Create()
            .Contains("description", "test")
            .Build();

        Assert.IsType<VectorFilterPredicate>(filter);
        var predicate = (VectorFilterPredicate)filter;
        Assert.Equal(FilterOperator.Contains, predicate.PredicateValue.Operator);
        Assert.Equal("test", predicate.PredicateValue.Value);
    }

    [Fact]
    public void DateRange_CreatesDateRangeFilter()
    {
        var minDate = new DateTime(2024, 1, 1);
        var maxDate = new DateTime(2024, 12, 31);
        
        var filter = VectorFilterBuilder.Create()
            .DateRange("createdAt", minDate, maxDate)
            .Build();

        Assert.IsType<VectorFilterAnd>(filter);
        var andFilter = (VectorFilterAnd)filter;
        Assert.Equal(2, andFilter.Filters.Count);
    }

    [Fact]
    public void NumericRange_CreatesNumericRangeFilter()
    {
        var filter = VectorFilterBuilder.Create()
            .NumericRange("score", 0.0, 100.0)
            .Build();

        Assert.IsType<VectorFilterAnd>(filter);
        var andFilter = (VectorFilterAnd)filter;
        Assert.Equal(2, andFilter.Filters.Count);
    }

    [Fact]
    public void ComplexFilter_CombinesMultipleOperations()
    {
        var filter = VectorFilterBuilder.Create()
            .Equal("tenantId", "tenant1")
            .AndGroup(group => group
                .Equal("status", "active")
                .OrGroup(orGroup => orGroup
                    .Equal("type", "document")
                    .Equal("type", "article")))
            .GreaterThan("score", 50)
            .And();

        Assert.IsType<VectorFilterAnd>(filter);
        var andFilter = (VectorFilterAnd)filter;
        Assert.True(andFilter.Filters.Count >= 2);
    }
}
