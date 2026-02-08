using Api.Application.Tenancy.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Product.MetricsProcessing.Gold;

[Index(nameof(UserId), nameof(Timestamp))] // The most important index for the CS Agent
public class MetricGoldEntity : TenantAwareEntity
{
    public Guid SilverId { get; set; } // The link to the "Deep Dive"

    // Identity
    public string UserId { get; set; } = null!;
    public string Url { get; set; } = null!;
    public DateTime Timestamp { get; set; }

    // Triage Indicators (The "Traffic Light" logic)
    public decimal LcpMs { get; set; }
    public decimal Cls { get; set; }
    public bool IsSlow { get; set; }        // Overall health flag
    public string HealthGrade { get; set; } = null!; // "Good", "Needs Improvement", "Poor"
}