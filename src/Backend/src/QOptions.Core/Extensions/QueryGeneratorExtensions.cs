using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using QOptions.Core.Models.Common;
using QOptions.Core.Models.Query;

namespace QOptions.Core.Extensions;

/// <summary>
/// Provides extension methods to create query options
/// </summary>
public static class QueryGeneratorExtensions
{
    #region Generating

    /// <summary>
    /// Generates query from a type
    /// </summary>
    /// <param name="sourceType">Query source type</param>
    /// <typeparam name="TEntity">Query source type</typeparam>
    /// <returns>Created query options</returns>
    public static IEntityQueryOptions<TEntity> CreateQuery<TEntity>(this TEntity sourceType) where TEntity : class, IQueryableEntity
    {
        ArgumentNullException.ThrowIfNull(sourceType);

        return new EntityQueryOptions<TEntity>();
    }

    /// <summary>
    /// Generates query from a type
    /// </summary>
    /// <param name="sourceType">Query source type</param>
    /// <typeparam name="TModel">Query source type</typeparam>
    /// <returns>Created query options</returns>
    public static IQueryOptions<TModel> CreateQuery<TModel>(this Type sourceType) where TModel : class
    {
        ArgumentNullException.ThrowIfNull(sourceType);

        return new QueryOptions<TModel>();
    }

    #endregion

    #region Adding search

    /// <summary>
    /// Adds search options to given query options
    /// </summary>
    /// <param name="options">The query options</param>
    /// <param name="keyword">Search keyword</param>
    /// <param name="includeChildren">Determines whether to include children</param>
    /// <typeparam name="TModel">Query source type</typeparam>
    /// <returns>Updated query options</returns>
    /// <exception cref="ArgumentNullException">If given options is null</exception>
    public static IQueryOptions<TModel> AddSearch<TModel>(this IQueryOptions<TModel> options, string keyword, bool includeChildren = false)
        where TModel : class
    {
        ArgumentNullException.ThrowIfNull(options);

        options.SearchOptions = new SearchOptions<TModel>(keyword, includeChildren);
        return options;
    }

    /// <summary>
    /// Adds search options to given query options
    /// </summary>
    /// <param name="options">The query options</param>
    /// <param name="keyword">Search keyword</param>
    /// <param name="includeChildren">Determines whether to include children</param>
    /// <typeparam name="TEntity">Query source type</typeparam>
    /// <returns>Updated query options</returns>
    /// <exception cref="ArgumentNullException">If given options is null</exception>
    public static IEntityQueryOptions<TEntity> AddSearch<TEntity>(
        this IEntityQueryOptions<TEntity> options,
        string keyword,
        bool includeChildren = false
    ) where TEntity : class, IQueryableEntity
    {
        ArgumentNullException.ThrowIfNull(options);

        options.SearchOptions = new SearchOptions<TEntity>(keyword, includeChildren);
        return options;
    }

    #endregion

    #region Adding filter

    /// <summary>
    /// Adds search options to given query options
    /// </summary>
    /// <param name="options">The query options</param>
    /// <param name="keySelector">Model filter property selector</param>
    /// <param name="value">Value to filter with</param>
    /// <typeparam name="TEntity">Query source type</typeparam>
    ///  <returns>Updated query options</returns>
    /// <exception cref="ArgumentNullException">If query options, key selector or value is null</exception>
    public static IEntityQueryOptions<TEntity> AddFilter<TEntity>(
        this IEntityQueryOptions<TEntity> options,
        Expression<Func<TEntity, object>> keySelector,
        object value
    ) where TEntity : class, IQueryableEntity
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(keySelector);

        // Get property name
        var memberExpression = keySelector.GetMemberExpression();
        var propertyName = memberExpression?.Member.Name ?? throw new InvalidOperationException("Member name is required to add filter options");

        // TODO : Check value to property type
        options.FilterOptions = options.FilterOptions ?? new FilterOptions<TEntity>();
        options.FilterOptions.Filters.Add(new QueryFilter(propertyName, value?.ToString()));

        return options;
    }

    /// <summary>
    /// Adds search options to given query options
    /// </summary>
    /// <param name="options">The query options</param>
    /// <param name="keySelector">Model filter property selector</param>
    /// <param name="value">Value to filter with</param>
    /// <typeparam name="TModel">Query source type</typeparam>
    /// <typeparam name="TOptions">Query options</typeparam>
    ///  <returns>Updated query options</returns>
    /// <exception cref="ArgumentNullException">If query options, key selector or value is null</exception>
    public static TOptions AddFilter<TModel, TOptions>(this TOptions options, Expression<Func<TModel, object>> keySelector, object value)
        where TModel : class where TOptions : IQueryOptions<TModel>
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(keySelector);

        // Get property name
        var memberExpression = keySelector.GetMemberExpression();
        var propertyName = memberExpression?.Member.Name ?? throw new InvalidOperationException("Member name is required to add filter options");

        // TODO : Check value to property type
        options.FilterOptions = options.FilterOptions ?? new FilterOptions<TModel>();
        options.FilterOptions.Filters.Add(new QueryFilter(propertyName, value.ToString()));

        return options;
    }

    #endregion

    #region Adding include

    /// <summary>
    /// Adds include options to given query options
    /// </summary>
    /// <param name="options">The query options</param>
    /// <param name="keySelector">Model filter property selector</param>
    /// <param name="value">Value to filter with</param>
    /// <typeparam name="TEntity">Query source type</typeparam>
    ///  <returns>Updated query options</returns>
    /// <exception cref="ArgumentNullException">If query options, key selector or value is null</exception>
    public static IEntityQueryOptions<TEntity> AddInclude<TEntity>(
        this IEntityQueryOptions<TEntity> options,
        Expression<Func<TEntity, IQueryableEntity>> keySelector
    ) where TEntity : class, IQueryableEntity
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(keySelector);

        // Get property name
        var memberExpression = keySelector.GetMemberExpression();
        var propertyName = memberExpression?.Member.Name ?? throw new InvalidOperationException("Member name is required to add include options");

        // TODO : Check value to property type
        if (options.IncludeOptions == null)
            options.IncludeOptions = new IncludeOptions<TEntity>(propertyName);
        else
            options.IncludeOptions.IncludeModels.Add(propertyName);

        return options;
    }

    /// <summary>
    /// Adds include options to given query options
    /// </summary>
    /// <param name="options">The query options</param>
    /// <param name="keySelector">Model filter property selector</param>
    /// <typeparam name="TEntity">Query source type</typeparam>
    ///  <returns>Updated query options</returns>
    /// <exception cref="ArgumentNullException">If query options, key selector or value is null</exception>
    public static IEntityQueryOptions<TEntity> AddInclude<TEntity>(
        this IEntityQueryOptions<TEntity> options,
        Expression<Func<TEntity, IEnumerable<IQueryableEntity>>> keySelector
    ) where TEntity : class, IQueryableEntity
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(keySelector);

        // Get property name
        var memberExpression = keySelector.GetMemberExpression();
        var propertyName = memberExpression?.Member.Name ?? throw new InvalidOperationException("Member name is required to add include options");

        // TODO : Check value to property type
        options.IncludeOptions = options.IncludeOptions ?? new IncludeOptions<TEntity>();
        options.IncludeOptions.IncludeModels.Add(propertyName);

        return options;
    }

    #endregion

    #region Adding sort

    /// <summary>
    /// Adds sort options to given query options
    /// </summary>
    /// <param name="options">The query options</param>
    /// <param name="keySelector">Model sort property selector</param>
    /// <param name="sortAscending">Value to filter with</param>
    /// <typeparam name="TModel">Query source type</typeparam>
    /// <typeparam name="TKey">Model property selector</typeparam>
    ///  <returns>Updated query options</returns>
    /// <exception cref="ArgumentNullException">If query options, key selector or value is null</exception>
    public static IQueryOptions<TModel> AddSort<TModel>(
        this IQueryOptions<TModel> options,
        Expression<Func<TModel, object>> keySelector,
        bool sortAscending
    ) where TModel : class
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(keySelector);

        // Get property name
        var memberExpression = keySelector.GetMemberExpression();
        var propertyName = memberExpression?.Member.Name ?? throw new InvalidOperationException("Member name is required to add sort options");

        options.SortOptions = new SortOptions<TModel>(propertyName, sortAscending);
        return options;
    }

    /// <summary>
    /// Adds sort options to given query options
    /// </summary>
    /// <param name="options">The query options</param>
    /// <param name="keySelector">Model sort property selector</param>
    /// <param name="sortAscending">Value to filter with</param>
    /// <typeparam name="TEntity">Query source type</typeparam>
    ///  <returns>Updated query options</returns>
    /// <exception cref="ArgumentNullException">If query options, key selector or value is null</exception>
    public static IEntityQueryOptions<TEntity> AddSort<TEntity>(
        this IEntityQueryOptions<TEntity> options,
        Expression<Func<TEntity, object>> keySelector,
        bool sortAscending
    ) where TEntity : class, IQueryableEntity
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(keySelector);

        // Get property name
        var memberExpression = keySelector.GetMemberExpression();
        var propertyName = memberExpression?.Member.Name ?? throw new InvalidOperationException("Member name is required to add sort options");

        options.SortOptions = new SortOptions<TEntity>(propertyName, sortAscending);
        return options;
    }

    #endregion

    #region Adding pagination

    /// <summary>
    /// Adds pagination options to given query options
    /// </summary>
    /// <param name="options">The query options</param>
    /// <param name="pageSize">Determines how many items should be selected</param>
    /// <param name="pageToken">Determines which section of items should be returned</param>
    /// <typeparam name="TEntity">Query source type</typeparam>
    ///  <returns>Updated query options</returns>
    /// <exception cref="ArgumentNullException">If query options, key selector or value is null</exception>
    public static IEntityQueryOptions<TEntity> AddPagination<TEntity>(this IEntityQueryOptions<TEntity> options, int pageSize, int pageToken)
        where TEntity : class, IQueryableEntity
    {
        ArgumentNullException.ThrowIfNull(options);

        options.PaginationOptions = new PaginationOptions(pageSize, pageToken);
        return options;
    }


    /// <summary>
    /// Adds pagination options to given query options
    /// </summary>
    /// <param name="options">The query options</param>
    /// <param name="pageSize">Determines how many items should be selected</param>
    /// <param name="pageToken">Determines which section of items should be returned</param>
    /// <typeparam name="TModel">Query source type</typeparam>
    ///  <returns>Updated query options</returns>
    /// <exception cref="ArgumentNullException">If query options, key selector or value is null</exception>
    public static IQueryOptions<TModel> AddPagination<TModel>(this IQueryOptions<TModel> options, int pageSize, int pageToken) where TModel : class
    {
        ArgumentNullException.ThrowIfNull(options);

        options.PaginationOptions = new PaginationOptions(pageSize, pageToken);
        return options;
    }

    #endregion
}