using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using QOptions.Core.Extensions;
using QOptions.Core.Models.Query;

namespace QOptions.Primitive.Extensions
{
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
        /// <returns>Queryable source</returns>
        public static IEnumerable<TModel> ApplyQuery<TModel>(this IEnumerable<TModel> source, IQueryOptions<TModel> queryOptions) where TModel : class
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

        /// <summary>
        /// Applies given query options to query source
        /// </summary>
        /// <param name="source">Query source</param>
        /// <param name="queryOptions">Query options</param>
        /// <typeparam name="TModel">Query source type</typeparam>
        /// <returns>Queryable source</returns>
        public static IQueryable<TModel> ApplyQuery<TModel>(this IQueryable<TModel> source, IQueryOptions<TModel> queryOptions) where TModel : class
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
        /// <typeparam name="TModel">Query source type</typeparam>
        /// <returns>Queryable source</returns>
        public static IEnumerable<TModel> ApplySearch<TModel>(this IEnumerable<TModel> source, SearchOptions<TModel> searchOptions)
            where TModel : class
        {
            if (source == null || searchOptions == null)
                throw new ArgumentException();

            return source.Where(searchOptions.GetSearchExpression().Compile());
        }

        /// <summary>
        /// Applies given searching options to query source
        /// </summary>
        /// <param name="source">Query source</param>
        /// <param name="searchOptions">Search options</param>
        /// <typeparam name="TModel">Query source type</typeparam>
        /// <returns>Queryable source</returns>
        public static IQueryable<TModel> ApplySearch<TModel>(this IQueryable<TModel> source, SearchOptions<TModel> searchOptions) where TModel : class
        {
            if (source == null || searchOptions == null)
                throw new ArgumentException();

            return source.Where(searchOptions.GetSearchExpression());
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
                            var property = properties.First(y => string.Equals(y.Name, x.Key, StringComparison.CurrentCultureIgnoreCase));
                            var member = Expression.PropertyOrField(parameter, x.Key);

                            // Create specific expression based on type
                            var compareMethod = property.PropertyType.GetCompareMethod();
                            var expectedType = compareMethod.GetParameters().First();

                            var argument = Expression.Convert(Expression.Constant(x.GetValue(property.PropertyType)), expectedType.ParameterType);
                            var methodCaller = Expression.Call(member, compareMethod, argument);
                            return Expression.Lambda<Func<TModel, bool>>(methodCaller, parameter);
                        })
                        .ToList();

                    multiChoicePredicates.ForEach(x => predicate = PredicateBuilder<TModel>.Or(predicate, x));
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
        /// <returns>Queryable source</returns>
        public static IEnumerable<TModel> ApplyFilter<TModel>(this IEnumerable<TModel> source, FilterOptions<TModel> filterOptions)
            where TModel : class
        {
            return source.Where(filterOptions.GetFilterExpression().Compile());
        }

        public static IQueryable<TModel> ApplyFilter<TModel>(this IQueryable<TModel> source, FilterOptions<TModel> filterOptions) where TModel : class
        {
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
        /// <returns>Queryable source</returns>
        public static IQueryable<TModel> ApplySort<TModel>(this IQueryable<TModel> source, SortOptions<TModel> sortOptions) where TModel : class
        {
            if (source == null || sortOptions == null)
                throw new ArgumentNullException();

            // Get the properties type  of entity
            var parameter = Expression.Parameter(typeof(TModel));
            var properties = typeof(TModel).GetProperties().Where(x => x.PropertyType.IsSimpleType()).ToList();

            // Apply sorting
            var matchingProperty = properties.FirstOrDefault(x => x.Name.ToLower() == sortOptions.SortField.ToLower());

            if (matchingProperty == null)
                return source;

            var memExp = Expression.Convert(Expression.PropertyOrField(parameter, matchingProperty.Name), typeof(object));
            var keySelector = Expression.Lambda<Func<TModel, dynamic>>(memExp, true, parameter);
            return sortOptions.SortAscending ? source.OrderBy(keySelector) : source.OrderByDescending(keySelector);
        }

        /// <summary>
        /// Applies given sorting options to query source
        /// </summary>
        /// <param name="source">Query source</param>
        /// <param name="sortOptions">Sort options</param>
        /// <typeparam name="TModel">Query source type</typeparam>
        /// <returns>Queryable source</returns>
        public static IEnumerable<TModel> ApplySort<TModel>(this IEnumerable<TModel> source, SortOptions<TModel> sortOptions) where TModel : class
        {
            if (source == null || sortOptions == null)
                throw new ArgumentNullException();

            // Get the properties type  of entity
            var parameter = Expression.Parameter(typeof(TModel));
            var properties = typeof(TModel).GetProperties().Where(x => x.PropertyType.IsSimpleType()).ToList();

            // Apply sorting
            var matchingProperty = properties.FirstOrDefault(x => x.Name.ToLower() == sortOptions.SortField.ToLower());

            if (matchingProperty == null)
                return source;

            var memExp = Expression.PropertyOrField(parameter, matchingProperty.Name);
            var keySelector = Expression.Lambda<Func<TModel, object>>(memExp, true, parameter).Compile();

            return sortOptions.SortAscending ? source.OrderBy(keySelector) : source.OrderByDescending(keySelector);
        }

        #endregion

        #region Pagination

        /// <summary>
        /// Applies given sorting options to query source
        /// </summary>
        /// <param name="source">Query source</param>
        /// <param name="paginationOptions">Sort options</param>
        /// <typeparam name="TModel">Query source type</typeparam>
        /// <returns>Queryable source</returns>
        public static IQueryable<TModel> ApplyPagination<TModel>(this IQueryable<TModel> source, PaginationOptions paginationOptions)
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
        /// <typeparam name="TModel">Query source type</typeparam>
        /// <returns>Queryable source</returns>
        public static IEnumerable<TModel> ApplyPagination<TModel>(this IEnumerable<TModel> source, PaginationOptions paginationOptions)
        {
            if (source == null || paginationOptions == null)
                throw new ArgumentNullException();

            return source.Skip((paginationOptions.PageToken - 1) * paginationOptions.PageSize).Take(paginationOptions.PageSize);
        }

        #endregion
    }
}