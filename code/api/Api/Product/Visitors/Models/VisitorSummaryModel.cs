namespace Api.Product.Visitors.Models;

/// <summary>
/// Summary information about a visitor's activity
/// </summary>
public class VisitorSummaryModel
{
    /// <summary>
    /// Unique user identifier (may be null for anonymous visitors)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Guest identifier for anonymous visitors (may be null for identified users)
    /// </summary>
    public string? GuestId { get; set; }

    /// <summary>
    /// List of browsers the visitor has used (empty in MVP - to be enhanced)
    /// </summary>
    public List<string> Browsers { get; set; } = new();

    /// <summary>
    /// List of operating systems the visitor has used (empty in MVP - to be enhanced)
    /// </summary>
    public List<string> OperatingSystems { get; set; } = new();

    /// <summary>
    /// Most recent page load timestamp for this visitor
    /// </summary>
    public DateTimeOffset LastSeenAt { get; set; }

    /// <summary>
    /// Total number of page loads recorded for this visitor
    /// </summary>
    public int TotalPageLoads { get; set; }
}
