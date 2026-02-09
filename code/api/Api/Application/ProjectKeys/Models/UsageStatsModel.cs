namespace Api.Application.ProjectKeys.Models;

/// <summary>
/// Model representing usage statistics for a tenant
/// </summary>
public class UsageStatsModel
{
    /// <summary>
    /// Total page loads consumed by the tenant (all time)
    /// </summary>
    public int TotalPageLoads { get; set; }

    /// <summary>
    /// Page loads consumed in the current month
    /// </summary>
    public int CurrentMonthPageLoads { get; set; }

    /// <summary>
    /// Timestamp of the last page load
    /// </summary>
    public DateTimeOffset? LastPageLoadAt { get; set; }
}
