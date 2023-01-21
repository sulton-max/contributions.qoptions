namespace QOptions.Models.Query;

/// <summary>
/// Represents queryable source query options
/// </summary>
public class QueryFilter
{
    public QueryFilter(string key, string value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>
    /// Field key name
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Filtering value
    /// </summary>
    public string Value { get; }
}