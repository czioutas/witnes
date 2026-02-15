using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Tenancy.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Product.MetricsProcessing.Aggregates;

[Table("user_page_stats")]
[Index(nameof(TenantId), nameof(UserId), nameof(PagePath), nameof(WindowType), IsUnique = true)]
public class UserPageStatsEntity : TenantAwareEntity
{
    public string UserId { get; set; } = null!;
    public string PagePath { get; set; } = null!;

    /// <summary>
    /// Window type: "last_10", "last_50", "last_200", "month_YYYY_MM", "year_YYYY"
    /// </summary>
    public string WindowType { get; set; } = null!;

    public int VisitCount { get; set; }
    public double AvgLoadTimeMs { get; set; }
    public double AvgDocTtfbMs { get; set; }
    public double AvgApiTtfbWorstMs { get; set; }
    public double AvgTransferSizeBytes { get; set; }
    public double AvgCdnLoadTimeMs { get; set; }

    // For monthly/yearly: store sum + count for incremental updates
    public double SumLoadTimeMs { get; set; }
    public double SumDocTtfbMs { get; set; }
    public double SumApiTtfbWorstMs { get; set; }
    public double SumTransferSizeBytes { get; set; }
    public double SumCdnLoadTimeMs { get; set; }
}
