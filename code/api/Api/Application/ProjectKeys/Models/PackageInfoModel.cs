namespace Api.Application.ProjectKeys.Models;

/// <summary>
/// Model representing tenant package and pricing information
/// </summary>
public class PackageInfoModel
{
    /// <summary>
    /// Name of the pricing plan
    /// </summary>
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the pricing plan
    /// </summary>
    public string PlanDescription { get; set; } = string.Empty;

    /// <summary>
    /// Monthly page loads limit
    /// </summary>
    public int PageLoadsLimit { get; set; }

    /// <summary>
    /// Current page loads used this month
    /// </summary>
    public int CurrentMonthPageLoads { get; set; }

    /// <summary>
    /// Monthly subscription price
    /// </summary>
    public decimal PricePerMonth { get; set; }

    /// <summary>
    /// Price per extra page load beyond the limit
    /// </summary>
    public decimal PricePerExtraRequest { get; set; }

    /// <summary>
    /// Renewal date for the current billing period
    /// </summary>
    public DateTime? RenewalDate { get; set; }

    /// <summary>
    /// Indicates if the tenant is active on this plan
    /// </summary>
    public bool IsActive { get; set; }
}
