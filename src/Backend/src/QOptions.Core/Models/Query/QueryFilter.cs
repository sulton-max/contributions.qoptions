namespace QOptions.Core.Models.Query;

/// <summary>
/// Represents filtering options filter unit
/// </summary>
public class QueryFilter
{
    public QueryFilter(string key, string? value) => (Key, Value) = (key, value);

    /// <summary>
    /// Field key name
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Filtering value
    /// </summary>
    public string? Value { get; }
}