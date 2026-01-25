using System;
using System.Collections.Generic;
using System.Linq;

namespace Bipins.AI.Core.Vector;

/// <summary>
/// Fluent builder for creating VectorFilter expressions.
/// </summary>
public class VectorFilterBuilder
{
    private readonly List<VectorFilter> _filters = new();

    /// <summary>
    /// Creates a new filter builder.
    /// </summary>
    public static VectorFilterBuilder Create() => new();

    /// <summary>
    /// Adds an equality predicate.
    /// </summary>
    public VectorFilterBuilder Equal(string field, object value)
    {
        _filters.Add(new VectorFilterPredicate(new FilterPredicate(field, FilterOperator.Eq, value)));
        return this;
    }

    /// <summary>
    /// Adds a not-equal predicate.
    /// </summary>
    public VectorFilterBuilder NotEqual(string field, object value)
    {
        _filters.Add(new VectorFilterPredicate(new FilterPredicate(field, FilterOperator.Ne, value)));
        return this;
    }

    /// <summary>
    /// Adds a greater-than predicate.
    /// </summary>
    public VectorFilterBuilder GreaterThan(string field, object value)
    {
        _filters.Add(new VectorFilterPredicate(new FilterPredicate(field, FilterOperator.Gt, value)));
        return this;
    }

    /// <summary>
    /// Adds a greater-than-or-equal predicate.
    /// </summary>
    public VectorFilterBuilder GreaterThanOrEqual(string field, object value)
    {
        _filters.Add(new VectorFilterPredicate(new FilterPredicate(field, FilterOperator.Gte, value)));
        return this;
    }

    /// <summary>
    /// Adds a less-than predicate.
    /// </summary>
    public VectorFilterBuilder LessThan(string field, object value)
    {
        _filters.Add(new VectorFilterPredicate(new FilterPredicate(field, FilterOperator.Lt, value)));
        return this;
    }

    /// <summary>
    /// Adds a less-than-or-equal predicate.
    /// </summary>
    public VectorFilterBuilder LessThanOrEqual(string field, object value)
    {
        _filters.Add(new VectorFilterPredicate(new FilterPredicate(field, FilterOperator.Lte, value)));
        return this;
    }

    /// <summary>
    /// Adds a contains predicate (text search).
    /// </summary>
    public VectorFilterBuilder Contains(string field, string value)
    {
        _filters.Add(new VectorFilterPredicate(new FilterPredicate(field, FilterOperator.Contains, value)));
        return this;
    }

    /// <summary>
    /// Adds a range predicate (between two values, inclusive).
    /// </summary>
    public VectorFilterBuilder Range(string field, object minValue, object maxValue)
    {
        _filters.Add(new VectorFilterPredicate(new FilterPredicate(field, FilterOperator.Gte, minValue)));
        _filters.Add(new VectorFilterPredicate(new FilterPredicate(field, FilterOperator.Lte, maxValue)));
        return this;
    }

    /// <summary>
    /// Adds a date range predicate.
    /// </summary>
    public VectorFilterBuilder DateRange(string field, DateTime minDate, DateTime maxDate)
    {
        return Range(field, minDate, maxDate);
    }

    /// <summary>
    /// Adds a numeric range predicate.
    /// </summary>
    public VectorFilterBuilder NumericRange(string field, double minValue, double maxValue)
    {
        return Range(field, minValue, maxValue);
    }

    /// <summary>
    /// Combines all added predicates with AND.
    /// </summary>
    public VectorFilter And()
    {
        if (_filters.Count == 0)
        {
            throw new InvalidOperationException("No filters added to builder");
        }
        if (_filters.Count == 1)
        {
            return _filters[0];
        }
        return new VectorFilterAnd(_filters);
    }

    /// <summary>
    /// Combines all added predicates with OR.
    /// </summary>
    public VectorFilter Or()
    {
        if (_filters.Count == 0)
        {
            throw new InvalidOperationException("No filters added to builder");
        }
        if (_filters.Count == 1)
        {
            return _filters[0];
        }
        return new VectorFilterOr(_filters);
    }

    /// <summary>
    /// Negates the filter.
    /// </summary>
    public VectorFilter Not()
    {
        if (_filters.Count == 0)
        {
            throw new InvalidOperationException("No filters added to builder");
        }
        var filter = _filters.Count == 1 ? _filters[0] : new VectorFilterAnd(_filters);
        return new VectorFilterNot(filter);
    }

    /// <summary>
    /// Builds the filter (defaults to AND combination).
    /// </summary>
    public VectorFilter Build()
    {
        return And();
    }

    /// <summary>
    /// Adds a filter directly (for nested groups).
    /// </summary>
    internal void AddFilter(VectorFilter filter)
    {
        _filters.Add(filter);
    }
}

/// <summary>
/// Extension methods for VectorFilterBuilder to support nested conditions.
/// </summary>
public static class VectorFilterBuilderExtensions
{
    /// <summary>
    /// Adds a nested AND group.
    /// </summary>
    public static VectorFilterBuilder AndGroup(this VectorFilterBuilder builder, Action<VectorFilterBuilder> configure)
    {
        var nestedBuilder = VectorFilterBuilder.Create();
        configure(nestedBuilder);
        var nestedFilter = nestedBuilder.And();
        builder.AddFilter(nestedFilter);
        return builder;
    }

    /// <summary>
    /// Adds a nested OR group.
    /// </summary>
    public static VectorFilterBuilder OrGroup(this VectorFilterBuilder builder, Action<VectorFilterBuilder> configure)
    {
        var nestedBuilder = VectorFilterBuilder.Create();
        configure(nestedBuilder);
        var nestedFilter = nestedBuilder.Or();
        builder.AddFilter(nestedFilter);
        return builder;
    }
}
