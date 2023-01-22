using Microsoft.EntityFrameworkCore;
using QOptions.Models.Common;
using QOptions.Models.Query;
using System.Linq.Expressions;

namespace QOptions.Extensions;

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
    /// <typeparam name="TEntity">Query source type</typeparam>
    /// <returns>Queryable source</returns>
    public static IQueryable<TEntity> ApplyQuery<TEntity>(this IQueryable<TEntity> source, IEntityQueryOptions<TEntity> queryOptions)
        where TEntity : class, IEntity
    {
        if (source == null || queryOptions == null)
            throw new ArgumentNullException();

        var result = source;

        if (queryOptions.SearchOptions != null)
            result = result.Search(queryOptions.SearchOptions);

        if (queryOptions.FilterOptions != null)
            result = result.Filter(queryOptions.FilterOptions);

        if (queryOptions.IncludeOptions != null)
            result = result.ApplyIncluding(queryOptions.IncludeOptions);

        if (queryOptions.SortOptions != null)
            result = result.Sort(queryOptions.SortOptions);

        result = result.Paginate(queryOptions.PaginationOptions);

        return result;
    }

    /// <summary>
    /// Applies given query options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="queryOptions">Query options</param>
    /// <typeparam name="TSource">Query source type</typeparam>
    /// <returns>Queryable source</returns>
    public static IQueryable<TSource> ApplyQuery<TSource>(this IQueryable<TSource> source, IQueryOptions<TSource> queryOptions) where TSource : class
    {
        if (source == null || queryOptions == null)
            throw new ArgumentNullException();

        var result = source;

        if (queryOptions.SearchOptions != null)
            result = result.Search(queryOptions.SearchOptions);

        if (queryOptions.FilterOptions != null)
            result = result.Filter(queryOptions.FilterOptions);

        if (queryOptions.SortOptions != null)
            result = result.Sort(queryOptions.SortOptions);

        result = result.Paginate(queryOptions.PaginationOptions);

        return result;
    }

    /// <summary>
    /// Applies given query options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="queryOptions">Query options</param>
    /// <typeparam name="TSource">Query source type</typeparam>
    /// <returns>Queryable source</returns>
    public static IEnumerable<TSource> ApplyQuery<TSource>(this IEnumerable<TSource> source, IQueryOptions<TSource> queryOptions)
        where TSource : class
    {
        if (source == null || queryOptions == null)
            throw new ArgumentNullException();

        var result = source;

        if (queryOptions.SearchOptions != null)
            result = result.Search(queryOptions.SearchOptions);

        if (queryOptions.FilterOptions != null)
            result = result.Filter(queryOptions.FilterOptions);

        if (queryOptions.SortOptions != null)
            result = result.Sort(queryOptions.SortOptions);

        result = result.Paginate(queryOptions.PaginationOptions);

        return result;
    }

    #endregion

    #region Searching

    /// <summary>
    /// Creates expression from filter options
    /// </summary>
    /// <param name="searchOptions">Filters</param>
    /// <typeparam name="TSource">Query source type</typeparam>
    /// <returns>Queryable source</returns>
    private static Expression<Func<TSource, bool>> GetSearchExpression<TSource>(this SearchOptions<TSource> searchOptions) where TSource : class
    {
        if (searchOptions == null)
            throw new ArgumentNullException();

        // Get the properties type of entity
        var parameter = Expression.Parameter(typeof(TSource));
        var searchableProperties = typeof(TSource).GetSearchableProperties();

        // Add searchable properties
        var predicates = searchableProperties?.Select(x =>
            {
                // Create predicate expression
                var member = Expression.PropertyOrField(parameter, x.Name);

                // Create specific expression based on type
                var compareMethod = x.PropertyType.GetCompareMethod(true);
                var argument = Expression.Constant(searchOptions.Keyword, x.PropertyType);
                var methodCaller = Expression.Call(member, compareMethod!, argument);
                return Expression.Lambda<Func<TSource, bool>>(methodCaller, parameter);
            })
            .ToList();

        // Join predicate expressions
        var finalExpression = PredicateBuilder<TSource>.False;
        predicates?.ForEach(x => finalExpression = PredicateBuilder<TSource>.Or(finalExpression, x));

        return finalExpression;
    }

    /// <summary>
    /// Applies given searching options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="searchOptions">Search options</param>
    /// <typeparam name="TSource">Query source type</typeparam>
    /// <returns>Queryable source</returns>
    public static IQueryable<TSource> Search<TSource>(this IQueryable<TSource> source, SearchOptions<TSource> searchOptions) where TSource : class
    {
        if (source == null || searchOptions == null)
            throw new ArgumentNullException();

        // Include direct child entities if they have searchable properties too
        var searchExpressions = searchOptions.GetSearchExpression();

        if (searchOptions.IncludeChildren && typeof(TSource).IsEntity())
        {
            var relatedEntityiesProperty = typeof(TSource).GetDirectChildEntities()
                ?.Select(x => new
                {
                    Entity = x,
                    SearchableProperties = x.GetSearchableProperties()
                });
            var matchingRelatedEntities = relatedEntityiesProperty?.Where(x => x.SearchableProperties.Any()).ToList();

            // Include models
            var predicates = matchingRelatedEntities?.Select(x =>
                {
                    // Include matching entities
                    source.Include(x.Entity.Name);

                    // Add matching entity predicates

                    var parameter = Expression.Parameter(typeof(TSource));

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
                        return Expression.Lambda<Func<TSource, bool>>(methodCaller, parameter);
                    });
                })
                .ToList();

            // Join predicate expressions
            predicates?.ForEach(x => x?.ToList().ForEach(y => searchExpressions = PredicateBuilder<TSource>.Or(searchExpressions, y)));
        }

        return source.Where(searchExpressions);
    }

    /// <summary>
    /// Applies given searching options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="searchOptions">Search options</param>
    /// <typeparam name="TSource">Query source type</typeparam>
    /// <returns>Queryable source</returns>
    public static IEnumerable<TSource> Search<TSource>(this IEnumerable<TSource> source, SearchOptions<TSource> searchOptions) where TSource : class
    {
        if (source == null || searchOptions == null)
            throw new ArgumentNullException();

        return source.Where(searchOptions.GetSearchExpression().Compile());
    }

    #endregion

    #region Filtering

    /// <summary>
    /// Creates expression from filter options
    /// </summary>
    /// <param name="filterOptions">Filters</param>
    /// <typeparam name="TSource">Query source type</typeparam>
    /// <returns>Queryable source</returns>
    private static Expression<Func<TSource, bool>> GetFilterExpression<TSource>(this FilterOptions<TSource> filterOptions) where TSource : class
    {
        if (filterOptions == null)
            throw new ArgumentNullException();

        // Get the properties type  of entity
        var parameter = Expression.Parameter(typeof(TSource));
        var properties = typeof(TSource).GetProperties().Where(x => x.PropertyType.IsSimpleType()).ToList();

        // Convert filters to predicate expressions
        var predicates = filterOptions.Filters.Where(x => properties.Any(y => y.Name.ToLower() == x.Key.ToLower()))
            .GroupBy(x => x.Key)
            .Select(x =>
            {
                // Create multi choice predicates
                var predicate = PredicateBuilder<TSource>.False;
                var multiChoicePredicates = x.Select(x =>
                    {
                        // Create predicate expression
                        var property = properties.First(y => y.Name.ToLower() == x.Key.ToLower());
                        var member = Expression.PropertyOrField(parameter, x.Key);

                        // Create specific expression based on type
                        var compareMethod = property.PropertyType.GetCompareMethod();
                        var argument = Expression.Convert(Expression.Constant(x.GetValue(property.PropertyType)), property.PropertyType);
                        var methodCaller = Expression.Call(member, compareMethod, argument);
                        return Expression.Lambda<Func<TSource, bool>>(methodCaller, parameter);
                    })
                    .ToList();

                multiChoicePredicates.ForEach(x => predicate = PredicateBuilder<TSource>.Or(predicate, x));
                return predicate;
            })
            .ToList();

        // Join predicate expressions\
        var finalExpression = PredicateBuilder<TSource>.True;
        predicates.ForEach(x => finalExpression = PredicateBuilder<TSource>.And(finalExpression, x));

        return finalExpression;
    }

    /// <summary>
    /// Applies given filter options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="filterOptions">Filter options</param>
    /// <typeparam name="TSource">Query source type</typeparam>
    /// <returns>Queryable source</returns>
    public static IQueryable<TSource> Filter<TSource>(this IQueryable<TSource> source, FilterOptions<TSource> filterOptions) where TSource : class
    {
        return source.Where(filterOptions.GetFilterExpression());
    }

    /// <summary>
    /// Applies given filter options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="filterOptions">Filter options</param>
    /// <typeparam name="TSource">Query source type</typeparam>
    /// <returns>Queryable source</returns>
    public static IEnumerable<TSource> Filter<TSource>(this IEnumerable<TSource> source, FilterOptions<TSource> filterOptions) where TSource : class
    {
        return source.Where(filterOptions.GetFilterExpression().Compile());
    }

    #endregion

    #region Including

    /// <summary>
    /// Applies given include models options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="includeOptions">Include models options</param>
    /// <typeparam name="TEntity">Query source type</typeparam>
    /// <returns>Queryable source</returns>
    public static IQueryable<TEntity> ApplyIncluding<TEntity>(this IQueryable<TEntity> source, IncludeOptions<TEntity> includeOptions)
        where TEntity : class, IEntity
    {
        if (source == null || includeOptions == null)
            throw new ArgumentNullException();

        // Get the properties type  of entity
        var parameter = Expression.Parameter(typeof(TEntity));
        var properties = typeof(TEntity).GetProperties();

        // Include models
        includeOptions.IncludeModels = includeOptions.IncludeModels.Select(x => x.ToLower());
        var includeModels = typeof(TEntity).GetDirectChildEntities().Where(x => includeOptions.IncludeModels.Contains(x.Name.ToLower())).ToList();

        includeModels.ForEach(x => { source = source.Include(x.Name); });

        return source;
    }

    #endregion

    #region Sorting

    /// <summary>
    /// Applies given sorting options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="sortOptions">Sort options</param>
    /// <typeparam name="TSource">Query source type</typeparam>
    /// <returns>Queryable source</returns>
    public static IQueryable<TSource> Sort<TSource>(this IQueryable<TSource> source, SortOptions<TSource> sortOptions) where TSource : class
    {
        if (source == null || sortOptions == null)
            throw new ArgumentNullException();

        // Get the properties type  of entity
        var parameter = Expression.Parameter(typeof(TSource));
        var properties = typeof(TSource).GetProperties().Where(x => x.PropertyType.IsSimpleType()).ToList();

        // Apply sorting
        var matchingProperty = properties.FirstOrDefault(x => x.Name.ToLower() == sortOptions.SortField.ToLower());

        if (matchingProperty == null)
            return source;

        var memExp = Expression.Convert(Expression.PropertyOrField(parameter, matchingProperty.Name), typeof(object));
        var keySelector = Expression.Lambda<Func<TSource, dynamic>>(memExp, true, parameter);
        return sortOptions.SortAscending ? source.OrderBy(keySelector) : source.OrderByDescending(keySelector);
    }

    /// <summary>
    /// Applies given sorting options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="sortOptions">Sort options</param>
    /// <typeparam name="TSource">Query source type</typeparam>
    /// <returns>Queryable source</returns>
    public static IEnumerable<TSource> Sort<TSource>(this IEnumerable<TSource> source, SortOptions<TSource> sortOptions) where TSource : class
    {
        if (source == null || sortOptions == null)
            throw new ArgumentNullException();

        // Get the properties type  of entity
        var parameter = Expression.Parameter(typeof(TSource));
        var properties = typeof(TSource).GetProperties().Where(x => x.PropertyType.IsSimpleType()).ToList();

        // Apply sorting
        var matchingProperty = properties.FirstOrDefault(x => x.Name.ToLower() == sortOptions.SortField.ToLower());

        if (matchingProperty == null)
            return source;

        var memExp = Expression.PropertyOrField(parameter, matchingProperty.Name);
        var keySelector = Expression.Lambda<Func<TSource, object>>(memExp, true, parameter).Compile();

        return sortOptions.SortAscending ? source.OrderBy(keySelector) : source.OrderByDescending(keySelector);
    }

    #endregion

    #region Pagination

    /// <summary>
    /// Applies given sorting options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="paginationOptions">Sort options</param>
    /// <typeparam name="TSource">Query source type</typeparam>
    /// <returns>Queryable source</returns>
    public static IQueryable<TSource> Paginate<TSource>(this IQueryable<TSource> source, PaginationOptions paginationOptions)
    {
        if (source == null || paginationOptions == null)
            throw new ArgumentNullException();

        return source.Skip((paginationOptions.PageToken - 1) * paginationOptions.PageSize).Take(paginationOptions.PageSize);
    }

    /// <summary>
    /// Applies given sorting options to query source
    /// </summary>
    /// <param name="source">Query source</param>
    /// <param name="paginationOptions">Sort options</param>
    /// <typeparam name="TSource">Query source type</typeparam>
    /// <returns>Queryable source</returns>
    public static IEnumerable<TSource> Paginate<TSource>(this IEnumerable<TSource> source, PaginationOptions paginationOptions)
    {
        if (source == null || paginationOptions == null)
            throw new ArgumentNullException();

        return source.Skip((paginationOptions.PageToken - 1) * paginationOptions.PageSize).Take(paginationOptions.PageSize);
    }

    #endregion
}