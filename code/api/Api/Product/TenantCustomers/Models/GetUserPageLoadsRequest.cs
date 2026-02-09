using Libs.Pagination;

namespace Api.Product.TenantCustomers.Models;

/// <summary>
/// Request model for retrieving page loads for a specific user
/// </summary>
public class GetUserPageLoadsRequest : PaginationRequest
{
    /// <summary>
    /// User ID to filter page loads for
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Optional start date filter (inclusive)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Optional end date filter (inclusive)
    /// </summary>
    public DateTime? EndDate { get; set; }
}
