namespace QOptions.Core.Models.Query;

/// <summary>
/// Represents queryable source query options
/// </summary>
/// <typeparam name="TModel">Query source type</typeparam>
public class QueryOptions<TModel> : IQueryOptions<TModel> where TModel : class
{
    public SearchOptions<TModel>? SearchOptions { get; set; }

    public FilterOptions<TModel>? FilterOptions { get; set; }

    public SortOptions<TModel>? SortOptions { get; set; }

    public PaginationOptions PaginationOptions { get; set; } = new PaginationOptions(10, 1);
}