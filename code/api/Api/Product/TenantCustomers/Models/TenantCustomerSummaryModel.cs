namespace Api.Product.TenantCustomers.Models;

/// <summary>
/// Summary information about a user's activity
/// </summary>
public class TenantCustomerSummaryModel
{
    /// <summary>
    /// Unique user identifier
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// List of browsers the user has used (empty in MVP - to be enhanced)
    /// </summary>
    public List<string> Browsers { get; set; } = new();

    /// <summary>
    /// List of operating systems the user has used (empty in MVP - to be enhanced)
    /// </summary>
    public List<string> OperatingSystems { get; set; } = new();

    /// <summary>
    /// Most recent page load timestamp for this user
    /// </summary>
    public DateTimeOffset LastSeenAt { get; set; }

    /// <summary>
    /// Total number of page loads recorded for this user
    /// </summary>
    public int TotalPageLoads { get; set; }
}
