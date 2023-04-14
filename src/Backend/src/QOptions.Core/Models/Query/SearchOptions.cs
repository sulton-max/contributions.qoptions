using System;

namespace QOptions.Core.Models.Query;

/// <summary>
/// Represents search options
/// </summary>
/// <typeparam name="TModel">Query source type</typeparam>
public class SearchOptions<TModel> where TModel : class
{
    public SearchOptions(string keyword, bool includeChildren) =>
        (Keyword, IncludeChildren) = (keyword ?? throw new ArgumentException("Search keyword is required to create search options"), includeChildren);

    /// <summary>
    /// Search keyword
    /// </summary>
    public string Keyword { get; }

    /// <summary>
    /// Determines whether to search from direct children
    /// </summary>
    public bool IncludeChildren { get; }
}