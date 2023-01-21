namespace QOptions.Models.Query;

/// <summary>
/// Represents search options
/// </summary>
public class SearchOptions<TSource> where TSource : class
{
    /// <summary>
    /// Search keyword
    /// </summary>
    public string Keyword { get; set; } = null!;

    /// <summary>
    /// Determines whether to search from direct children
    /// </summary>
    public bool IncludeChildren { get; set; }
}