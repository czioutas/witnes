using System.ComponentModel.DataAnnotations;

namespace Api.Application.ProjectKeys.Models;

/// <summary>
/// Model representing tenant package and pricing information
/// </summary>
public class PackageInfoModel
{
    /// <summary>
    /// Name of the pricing plan
    /// </summary>
    [Required]
    public required string PlanName { get; set; }

    /// <summary>
    /// Description of the pricing plan
    /// </summary>
    [Required]
    public required string PlanDescription { get; set; }

    /// <summary>
    /// Monthly page load limit
    /// </summary>
    [Required]
    public required int MonthlyPageLoadLimit { get; set; }

    /// <summary>
    /// Current page loads used this month
    /// </summary>
    [Required]
    public required int CurrentMonthPageLoads { get; set; }

    /// <summary>
    /// Monthly subscription price
    /// </summary>
    [Required]
    public required decimal PricePerMonth { get; set; }

    /// <summary>
    /// Maximum team members allowed
    /// </summary>
    [Required]
    public int MaxTeamMembers { get; set; }

    /// <summary>
    /// Data retention in days
    /// </summary>
    [Required]
    public int DataRetentionDays { get; set; }

    /// <summary>
    /// Renewal date for the current billing period
    /// </summary>
    public DateTime? RenewalDate { get; set; }

    /// <summary>
    /// Indicates if the tenant is active on this plan
    /// </summary>
    [Required]
    public required bool IsActive { get; set; }
}
