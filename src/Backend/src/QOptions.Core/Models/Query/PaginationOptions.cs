namespace QOptions.Models.Query;

/// <summary>
/// Represents pagination options
/// </summary>
public class PaginationOptions
{
    private int _pageSize;
    private int _pageToken;

    public PaginationOptions(int pageSize, int pageToken)
    {
        PageSize = pageSize;
        PageToken = pageToken;
    }

    /// <summary>
    /// Current page size
    /// </summary>
    /// <exception cref="ArgumentException">If value is invalid</exception>
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (value <= 0)
                throw new ArgumentException();
            _pageSize = value;
        }
    }

    /// <summary>
    /// Current page token
    /// </summary>
    /// <exception cref="ArgumentException">If value is invalid</exception>
    public int PageToken
    {
        get => _pageToken;
        set
        {
            if (value <= 0)
                throw new ArgumentException();
            _pageToken = value;
        }
    }
}