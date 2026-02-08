namespace Libs.Pagination;

/// <summary>
/// Standard pagination request parameters
/// </summary>
public class PaginationRequest
{
    protected virtual int MaxPageSize => 100;
    protected virtual int DefaultPageSize => 20;

    private int _pageNumber = 1;
    private int _pageSize;

    public PaginationRequest()
    {
        _pageSize = DefaultPageSize;
    }

    /// <summary>
    /// Page number (1-indexed)
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Number of items per page (max 100)
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? DefaultPageSize : value);
    }

    /// <summary>
    /// Calculate skip count for database queries
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Get the take/limit count for database queries
    /// </summary>
    public int Take => PageSize;
}
