using Libs.Pagination;

namespace Api.Product.TenantCustomers.Models;

/// <summary>
/// Request model for filtering users
/// </summary>
public class GetTenantCustomersRequest : PaginationRequest
{
    /// <summary>
    /// Search filter for user IDs (case-insensitive contains)
    /// </summary>
    public string? UserIdSearch { get; set; }

    /// <summary>
    /// Filter by page loads after this date (inclusive)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Filter by page loads before this date (inclusive)
    /// </summary>
    public DateTime? EndDate { get; set; }
}
