using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using QOptions.Core.Extensions;
using QOptions.Core.Models.Common;
using QOptions.Core.Models.Query;
using QOptions.Primitive.Extensions;

namespace QOptions.Relational.Extensions
{
    /// <summary>
    /// Provides extension methods to create complex relational queries
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
            where TEntity : class, IQueryableEntity
        {
            if (source == null || queryOptions == null)
                throw new ArgumentNullException();

            var result = source;

            if (queryOptions.SearchOptions != null)
                result = result.ApplySearch(queryOptions.SearchOptions);

            if (queryOptions.FilterOptions != null)
                result = result.ApplyFilter(queryOptions.FilterOptions);

            if (queryOptions.SortOptions != null)
                result = result.ApplySort(queryOptions.SortOptions);

            if (queryOptions.PaginationOptions != null)
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
        /// <returns>Queryable source</returns>
        public static Expression<Func<TModel, bool>> GetSearchExpression<TModel>(this SearchOptions<TModel> searchOptions) where TModel : class
        {
            if (searchOptions == null)
                throw new ArgumentNullException();

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
        /// <typeparam name="TEntity">Query source type</typeparam>
        /// <returns>Queryable source</returns>
        public static IQueryable<TEntity> ApplySearch<TEntity>(this IQueryable<TEntity> source, SearchOptions<TEntity> searchOptions)
            where TEntity : class, IQueryableEntity
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(searchOptions);

            // Include direct child entities if they have searchable properties too
            var searchExpressions = searchOptions.GetSearchExpression();

            if (searchOptions.IncludeChildren && typeof(TEntity).IsEntity())
            {
                var relatedEntityiesProperty = typeof(TEntity).GetDirectChildEntities()
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
            }

            return source.Where(searchExpressions);
        }

        #endregion

        #region Filtering

        /// <summary>
        /// Creates expression from filter options
        /// </summary>
        /// <param name="filterOptions">Filters</param>
        /// <typeparam name="TModel">Query source type</typeparam>
        /// <returns>Queryable source</returns>
        public static Expression<Func<TModel, bool>> GetFilterExpression<TModel>(this FilterOptions<TModel> filterOptions) where TModel : class
        {
            if (filterOptions == null)
                throw new ArgumentNullException();

            // Get the properties type  of entity
            var parameter = Expression.Parameter(typeof(TModel));
            var properties = typeof(TModel).GetProperties().Where(x => x.PropertyType.IsSimpleType()).ToList();

            // Convert filters to predicate expressions
            var predicates = filterOptions.Filters.Where(x => properties.Any(y => y.Name.ToLower() == x.Key.ToLower()))
                .GroupBy(x => x.Key)
                .Select(x =>
                {
                    // Create multi choice predicates
                    var predicate = PredicateBuilder<TModel>.False;
                    var multiChoicePredicates = x.Select(x =>
                        {
                            // Create predicate expression
                            var property = properties.First(y => y.Name.ToLower() == x.Key.ToLower());
                            var member = Expression.PropertyOrField(parameter, x.Key);

                            // Create specific expression based on type
                            var compareMethod = property.PropertyType.GetCompareMethod();
                            var argument = Expression.Convert(Expression.Constant(x.GetValue(property.PropertyType)), property.PropertyType);
                            var methodCaller = Expression.Call(member, compareMethod, argument);
                            return Expression.Lambda<Func<TModel, bool>>(methodCaller, parameter);
                        })
                        .ToList();

                    multiChoicePredicates.ForEach(x => predicate = PredicateBuilder<TModel>.Or(predicate, x));
                    return predicate;
                })
                .ToList();

            // Join predicate expressions
            var finalExpression = PredicateBuilder<TModel>.True;
            predicates.ForEach(x => finalExpression = PredicateBuilder<TModel>.And(finalExpression, x));

            return finalExpression;
        }

        /// <summary>
        /// Applies given filter options to query source
        /// </summary>
        /// <param name="source">Query source</param>
        /// <param name="filterOptions">Filter options</param>
        /// <typeparam name="TEntity">Query source type</typeparam>
        /// <returns>Queryable source</returns>
        public static IQueryable<TEntity> ApplyEntityFilter<TEntity>(this IQueryable<TEntity> source, FilterOptions<TEntity> filterOptions)
            where TEntity : class, IQueryableEntity
        {
            return source.Where(filterOptions.GetFilterExpression());
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
            where TEntity : class, IQueryableEntity
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(includeOptions);

            // Get the properties type  of entity
            var parameter = Expression.Parameter(typeof(TEntity));
            var properties = typeof(TEntity).GetProperties();

            // Include models
            includeOptions.IncludeModels = includeOptions.IncludeModels.Select(x => x.ToLower()).ToList();
            var includeModels = typeof(TEntity).GetDirectChildEntities().Where(x => includeOptions.IncludeModels.Contains(x.Name.ToLower())).ToList();

            includeModels.ForEach(x => { source = source.Include(x.Name); });

            return source;
        }

        #endregion
    }
}