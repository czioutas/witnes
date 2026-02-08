using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Tenancy.Entities;

namespace Api.Product.MetricsProcessing.Bronze;

public class MetricBronzeEntity : TenantAwareEntity
{
    // --- SEARCHABLE & STATIC (The "Known" Schema) ---
    // These are extracted during ingestion for fast querying.
    public string UserId { get; set; } = null!;
    public string SessionId { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string EventType { get; set; } = null!; // LOAD, SPA_NAV

    // --- THE "BLACK BOX" (The "Dynamic" Schema) ---
    // This allows the waterfall or vitals to change without a migration.
    [Column(TypeName = "jsonb")]
    public string RawPerformanceData { get; set; } = null!;

    // --- AUDIT ---
    public DateTime IngestedAt { get; set; }
}