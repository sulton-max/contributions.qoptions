using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using QOptions.Core.Extensions;
using QOptions.Core.Models.Common;
using QOptions.Core.Models.Query;

namespace QOptions.Query.Extensions;

/// <summary>
/// Provides extension methods to create complex queries
/// </summary>
public static class QueryExtensions
{
    #region Querying

    /// <summary>
    /// Applies given query options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="queryOptions">Query options</param>
    /// <typeparam name="TModel">Query source type</typeparam>
    /// <exception cref="ArgumentNullException">If source or query options is null</exception>
    /// <returns>Queryable source</returns>
    public static IEnumerable<TModel> ApplyQuery<TModel>(this IEnumerable<TModel> source, IQueryOptions<TModel> queryOptions) where TModel : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(queryOptions);

        var result = source;

        if (queryOptions.SearchOptions != null)
            result = result.ApplySearch(queryOptions.SearchOptions);

        if (queryOptions.FilterOptions != null)
            result = result.ApplyFilter(queryOptions.FilterOptions);

        if (queryOptions.SortOptions != null)
            result = result.ApplySort(queryOptions.SortOptions);

        result = result.ApplyPagination(queryOptions.PaginationOptions);

        return result;
    }

    /// <summary>
    /// Applies given query options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="queryOptions">Query options</param>
    /// <typeparam name="TModel">Query source type</typeparam>
    /// <exception cref="ArgumentNullException">If source or query options is null</exception>
    /// <returns>Queryable source</returns>
    public static IQueryable<TModel> ApplyQuery<TModel>(this IQueryable<TModel> source, IQueryOptions<TModel> queryOptions) where TModel : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(queryOptions);

        var result = source;

        if (queryOptions.SearchOptions != null)
            result = result.ApplySearch(queryOptions.SearchOptions);

        if (queryOptions.FilterOptions != null)
            result = result.ApplyFilter(queryOptions.FilterOptions);

        if (queryOptions.SortOptions != null)
            result = result.ApplySort(queryOptions.SortOptions);

        result = result.ApplyPagination(queryOptions.PaginationOptions);

        return result;
    }

    /// <summary>
    /// Applies given query options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="queryOptions">Query options</param>
    /// <typeparam name="TEntity">Query source type</typeparam>
    /// <exception cref="ArgumentNullException">If source or query options is null</exception>
    /// <returns>Queryable source</returns>
    public static IQueryable<TEntity> ApplyQuery<TEntity>(this IQueryable<TEntity> source, IEntityQueryOptions<TEntity> queryOptions)
        where TEntity : class, IQueryableEntity
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(queryOptions);

        var result = source;

        if (queryOptions.SearchOptions != null)
            result = result.ApplySearch(queryOptions.SearchOptions);

        if (queryOptions.FilterOptions != null)
            result = result.ApplyFilter(queryOptions.FilterOptions);

        if (queryOptions.SortOptions != null)
            result = result.ApplySort(queryOptions.SortOptions);

        result = result.ApplyPagination(queryOptions.PaginationOptions);

        return result;
    }

    #endregion

    #region Searching

    /// <summary>
    /// Creates expression from filter options
    /// </summary>
    /// <param name="searchOptions">Filters</param>
    /// <typeparam name="TModel">Query source type</typeparam>
    /// <exception cref="ArgumentNullException">If search options is null</exception>
    /// <returns>Queryable source</returns>
    internal static Expression<Func<TModel, bool>> GetSearchExpression<TModel>(this SearchOptions<TModel> searchOptions) where TModel : class
    {
        ArgumentNullException.ThrowIfNull(searchOptions);

        // Get the properties type of entity
        var parameter = Expression.Parameter(typeof(TModel));
        var searchableProperties = typeof(TModel).GetSearchableProperties();

        // Add searchable properties
        var predicates = searchableProperties?.Select(x =>
            {
                // Create predicate expression
                var member = Expression.PropertyOrField(parameter, x.Name);

                // Create specific expression based on type
                var compareMethod = x.PropertyType.GetCompareMethod(true);
                var argument = Expression.Constant(searchOptions.Keyword, x.PropertyType);
                var methodCaller = Expression.Call(member, compareMethod!, argument);
                return Expression.Lambda<Func<TModel, bool>>(methodCaller, parameter);
            })
            .ToList();

        // Join predicate expressions
        var finalExpression = PredicateBuilder<TModel>.False;
        predicates?.ForEach(x => finalExpression = PredicateBuilder<TModel>.Or(finalExpression, x));

        return finalExpression;
    }

    /// <summary>
    /// Applies given searching options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="searchOptions">Search options</param>
    /// <typeparam name="TModel">Query source type</typeparam>
    /// <exception cref="ArgumentNullException">If source or search options is null</exception>
    /// <returns>Queryable source</returns>
    public static IEnumerable<TModel> ApplySearch<TModel>(this IEnumerable<TModel> source, SearchOptions<TModel> searchOptions) where TModel : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(searchOptions);

        return source.Where(searchOptions.GetSearchExpression().Compile());
    }

    /// <summary>
    /// Applies given searching options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="searchOptions">Search options</param>
    /// <typeparam name="TModel">Query source type</typeparam>
    /// <exception cref="ArgumentNullException">If source or search options is null</exception>
    /// <returns>Queryable source</returns>
    public static IQueryable<TModel> ApplySearch<TModel>(this IQueryable<TModel> source, SearchOptions<TModel> searchOptions) where TModel : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(searchOptions);

        var searchExpressions = searchOptions.GetSearchExpression();

        // Include direct child entities if they have searchable properties too
        if (source is IQueryable<IQueryableEntity> entitySource && searchOptions is SearchOptions<IQueryableEntity> entitySearchOptions &&
            searchExpressions is Expression<Func<IQueryableEntity, bool>> entitySearchExpressions)
            entitySearchExpressions.AddSearchIncludeExpressions(entitySearchOptions, entitySource);

        return source.Where(searchOptions.GetSearchExpression());
    }

    #endregion

    #region Filtering

    /// <summary>
    /// Creates expression from filter options
    /// </summary>
    /// <param name="filterOptions">Filters</param>
    /// <typeparam name="TModel">Query source type</typeparam>
    /// <exception cref="ArgumentNullException">If filter options is null</exception>
    /// <returns>Queryable source</returns>
    internal static Expression<Func<TModel, bool>> GetFilterExpression<TModel>(this FilterOptions<TModel> filterOptions) where TModel : class
    {
        ArgumentNullException.ThrowIfNull(filterOptions);

        // Get the properties type  of entity
        var parameter = Expression.Parameter(typeof(TModel));
        var properties = typeof(TModel).GetProperties().Where(x => x.PropertyType.IsSimpleType()).ToList();

        // Convert filters to predicate expressions
        var predicates = filterOptions.Filters
            .Where(x => properties.Any(y => string.Equals(y.Name, x.Key, StringComparison.CurrentCultureIgnoreCase)))
            .GroupBy(x => x.Key)
            .Select(x =>
            {
                // Create multi choice predicates
                var predicate = PredicateBuilder<TModel>.False;
                var multiChoicePredicates = x.Select(y =>
                    {
                        // Create predicate expression
                        var property = properties.First(z => string.Equals(z.Name, x.Key, StringComparison.CurrentCultureIgnoreCase));
                        var member = Expression.PropertyOrField(parameter, y.Key);

                        // Create specific expression based on type
                        var compareMethod = property.PropertyType.GetCompareMethod();
                        var expectedType = compareMethod.GetParameters().First();

                        var argument = Expression.Convert(Expression.Constant(y.GetValue(property.PropertyType)), expectedType.ParameterType);
                        var methodCaller = Expression.Call(member, compareMethod, argument);
                        return Expression.Lambda<Func<TModel, bool>>(methodCaller, parameter);
                    })
                    .ToList();

                multiChoicePredicates.ForEach(y => predicate = PredicateBuilder<TModel>.Or(predicate, y));
                return predicate;
            })
            .ToList();

        // Join predicate expressions\
        var finalExpression = PredicateBuilder<TModel>.True;
        predicates.ForEach(x => finalExpression = PredicateBuilder<TModel>.And(finalExpression, x));

        return finalExpression;
    }

    /// <summary>
    /// Applies given filter options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="filterOptions">Filter options</param>
    /// <typeparam name="TModel">Query source type</typeparam>
    /// <exception cref="ArgumentNullException">If source or filter options is null</exception>
    /// <returns>Queryable source</returns>
    public static IEnumerable<TModel> ApplyFilter<TModel>(this IEnumerable<TModel> source, FilterOptions<TModel> filterOptions) where TModel : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(filterOptions);

        return source.Where(filterOptions.GetFilterExpression().Compile());
    }

    /// <summary>
    /// Applies given filter options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="filterOptions">Filter options</param>
    /// <typeparam name="TEntity">Query source type</typeparam>
    /// <returns>Queryable source</returns>
    public static IQueryable<TModel> ApplyFilter<TModel>(this IQueryable<TModel> source, FilterOptions<TModel> filterOptions) where TModel : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(filterOptions);

        return source.Where(filterOptions.GetFilterExpression());
    }

    #endregion

    #region Sorting

    /// <summary>
    /// Applies given sorting options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="sortOptions">Sort options</param>
    /// <typeparam name="TModel">Query source type</typeparam>
    /// <exception cref="ArgumentNullException">If source or sort options is null</exception>
    /// <returns>Queryable source</returns>
    public static IQueryable<TModel> ApplySort<TModel>(this IQueryable<TModel> source, SortOptions<TModel> sortOptions) where TModel : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sortOptions);

        // Get the properties type  of entity
        var parameter = Expression.Parameter(typeof(TModel));
        var properties = typeof(TModel).GetProperties().Where(x => x.PropertyType.IsSimpleType()).ToList();

        // Apply sorting
        var matchingProperty = properties.FirstOrDefault(x => x.Name.ToLower() == sortOptions.SortField.ToLower());

        if (matchingProperty == null)
            return source;

        var memExp = Expression.Convert(Expression.PropertyOrField(parameter, matchingProperty.Name), typeof(object));
        var keySelector = Expression.Lambda<Func<TModel, dynamic>>(memExp, true, parameter);
        return sortOptions.IsAscending ? source.OrderBy(keySelector) : source.OrderByDescending(keySelector);
    }

    /// <summary>
    /// Applies given sorting options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="sortOptions">Sort options</param>
    /// <typeparam name="TModel">Query source type</typeparam>
    /// <exception cref="ArgumentNullException">If source or sort options is null</exception>
    /// <returns>Queryable source</returns>
    public static IEnumerable<TModel> ApplySort<TModel>(this IEnumerable<TModel> source, SortOptions<TModel> sortOptions) where TModel : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sortOptions);

        // Get the properties type  of entity
        var parameter = Expression.Parameter(typeof(TModel));
        var properties = typeof(TModel).GetProperties().Where(x => x.PropertyType.IsSimpleType()).ToList();

        // Apply sorting
        var matchingProperty = properties.FirstOrDefault(x => x.Name.ToLower() == sortOptions.SortField.ToLower());

        if (matchingProperty == null)
            return source;

        var memExp = Expression.PropertyOrField(parameter, matchingProperty.Name);
        var keySelector = Expression.Lambda<Func<TModel, object>>(memExp, true, parameter).Compile();

        return sortOptions.IsAscending ? source.OrderBy(keySelector) : source.OrderByDescending(keySelector);
    }

    #endregion

    #region Including

    /// <summary>
    /// Adds include expressions to search expression
    /// </summary>
    /// <param name="searchExpressions">Search expression to modify</param>
    /// <param name="searchOptions">Search options to modify search expressions</param>
    /// <param name="source">Queryable source</param>
    /// <typeparam name="TEntity">Query source type</typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException">If source or search options is null</exception>
    internal static Expression<Func<TEntity, bool>> AddSearchIncludeExpressions<TEntity>(
        this Expression<Func<TEntity, bool>> searchExpressions,
        SearchOptions<TEntity> searchOptions,
        IQueryable<TEntity> source
    ) where TEntity : class, IQueryableEntity
    {
        if (searchOptions == null || source == null)
            throw new ArgumentException("Can't create search include expressions to null source or with null search options");

        var relatedEntitiesProperty = typeof(TEntity).GetDirectChildEntities()
            ?.Select(x => new
            {
                Entity = x,
                SearchableProperties = x.GetSearchableProperties()
            });
        var matchingRelatedEntities = relatedEntitiesProperty?.Where(x => x.SearchableProperties.Any()).ToList();

        // Include models
        var predicates = matchingRelatedEntities?.Select(x =>
            {
                // Include matching entities
                source.Include(x.Entity.Name);

                // Add matching entity predicates
                var parameter = Expression.Parameter(typeof(TEntity));

                // Add searchable properties
                return x.SearchableProperties?.Select(y =>
                {
                    // Create predicate expression
                    var entity = Expression.PropertyOrField(parameter, x.Entity.Name);
                    var entityProperty = Expression.PropertyOrField(entity, y.Name);

                    // Create specific expression based on type
                    var compareMethod = y.PropertyType.GetCompareMethod(true);
                    var argument = Expression.Constant(searchOptions.Keyword, y.PropertyType);
                    var methodCaller = Expression.Call(entityProperty, compareMethod!, argument);
                    return Expression.Lambda<Func<TEntity, bool>>(methodCaller, parameter);
                });
            })
            .ToList();

        // Join predicate expressions
        predicates?.ForEach(x => x?.ToList().ForEach(y => searchExpressions = PredicateBuilder<TEntity>.Or(searchExpressions, y)));
        return searchExpressions;
    }

    /// <summary>
    /// Applies given include models options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="includeOptions">Include models options</param>
    /// <typeparam name="TEntity">Query source type</typeparam>
    /// <exception cref="ArgumentNullException">If source or include options is null</exception>
    /// <returns>Queryable source</returns>
    public static IQueryable<TEntity> ApplyIncluding<TEntity>(this IQueryable<TEntity> source, IncludeOptions<TEntity> includeOptions)
        where TEntity : class, IQueryableEntity
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(includeOptions);

        // Include models
        includeOptions.IncludeModels = includeOptions.IncludeModels.Select(x => x.ToLower()).ToList();
        var includeModels = typeof(TEntity).GetDirectChildEntities().Where(x => includeOptions.IncludeModels.Contains(x.Name.ToLower())).ToList();

        includeModels.ForEach(x => { source = source.Include(x.Name); });

        return source;
    }

    #endregion

    #region Pagination

    /// <summary>
    /// Applies given sorting options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="paginationOptions">Sort options</param>
    /// <typeparam name="TModel">Query source type</typeparam>
    /// <exception cref="ArgumentNullException">If source or pagination options is null</exception>
    /// <returns>Queryable source</returns>
    public static IQueryable<TModel> ApplyPagination<TModel>(this IQueryable<TModel> source, PaginationOptions paginationOptions)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(paginationOptions);

        return source.Skip((paginationOptions.PageToken - 1) * paginationOptions.PageSize).Take(paginationOptions.PageSize);
    }

    /// <summary>
    /// Applies given sorting options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="paginationOptions">Sort options</param>
    /// <typeparam name="TModel">Query source type</typeparam>
    /// <exception cref="ArgumentNullException">If source or pagination options is null</exception>
    /// <returns>Queryable source</returns>
    public static IEnumerable<TModel> ApplyPagination<TModel>(this IEnumerable<TModel> source, PaginationOptions paginationOptions)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(paginationOptions);

        return source.Skip((paginationOptions.PageToken - 1) * paginationOptions.PageSize).Take(paginationOptions.PageSize);
    }

    #endregion
}