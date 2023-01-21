namespace QOptions.Models.Query;

public class SortOptions<TSource>
{
    /// <summary>
    /// Sort field
    /// </summary>
    public string SortField { get; set; } = null!;

    /// <summary>
    /// Indicates whether to sort ascending
    /// </summary>
    public bool SortAscending { get; set; }
}