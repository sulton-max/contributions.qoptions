using System.Collections.Generic;
using System.Linq;

namespace QOptions.Core.Models.Query;

/// <summary>
/// Represents filtering options
/// </summary>
/// <typeparam name="TModel">Query source type</typeparam>
public class FilterOptions<TModel> where TModel : class
{
    public FilterOptions() => (Filters) = new List<QueryFilter>();

    public FilterOptions(string memberName, string? value) =>
        (Filters) = new List<QueryFilter>
        {
            new(memberName, value)
        };

    public List<QueryFilter> Filters { get; set; }

    public QueryFilter? this[string key]
    {
        get { return Filters.FirstOrDefault(x => x.Key == key); }
    }
}