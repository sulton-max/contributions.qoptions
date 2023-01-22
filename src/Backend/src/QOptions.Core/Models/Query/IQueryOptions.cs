namespace QOptions.Models.Query;

/// <summary>
/// Defines properties for queryable source query options
/// </summary>
/// <typeparam name="TSource">Query source type</typeparam>
public interface IQueryOptions<TSource> where TSource : class
{
    /// <summary>
    /// Query searching options
    /// </summary>
    SearchOptions<TSource>? SearchOptions { get; set; }

    /// <summary>
    /// Applied filters for a query
    /// </summary>
    FilterOptions<TSource>? FilterOptions { get; set; }

    /// <summary>
    /// Result Sort options 
    /// </summary>
    SortOptions<TSource>? SortOptions { get; set; }

    /// <summary>
    /// Calculated pagination options
    /// </summary>
    PaginationOptions PaginationOptions { get; set; }
}