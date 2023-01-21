namespace QOptions.Models.Query;

/// <summary>
/// Represents filtering options
/// </summary>
/// <typeparam name="TSource"></typeparam>
public class FilterOptions<TSource> where TSource : class
{
    public FilterOptions(ICollection<QueryFilter> filters)
    {
        Filters = filters;
    }

    public FilterOptions()
    {
        Filters = new List<QueryFilter>();
    }

    public IEnumerable<QueryFilter> Filters { get; set; }

    public QueryFilter? this[string key]
    {
        get { return Filters.FirstOrDefault(x => x.Key == key); }
    }
}