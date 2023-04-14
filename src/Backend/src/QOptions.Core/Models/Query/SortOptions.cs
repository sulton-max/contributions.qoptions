namespace QOptions.Core.Models.Query;

/// <summary>
/// Represents sort options
/// </summary>
/// <typeparam name="TModel">Query source type</typeparam>
public class SortOptions<TModel>
{
    public SortOptions(string sortField, bool sortAscending = true) => (SortField, IsAscending) = (sortField, sortAscending);

    /// <summary>
    /// Sort field
    /// </summary>
    public string SortField { get; }

    /// <summary>
    /// Indicates whether to sort ascending
    /// </summary>
    public bool IsAscending { get; }
}