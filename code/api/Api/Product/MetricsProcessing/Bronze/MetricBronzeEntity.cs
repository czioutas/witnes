using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Tenancy.Entities;

namespace Api.Product.MetricsProcessing.Bronze;

[Table("metrics_bronze")]
public class MetricBronzeEntity : TenantAwareEntity
{
    // --- SEARCHABLE & STATIC (The "Known" Schema) ---
    // These are extracted during ingestion for fast querying.
    public string? UserId { get; set; }
    public string HashId { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string EventType { get; set; } = null!; // LOAD, SPA_NAV

    // --- THE "BLACK BOX" (The "Dynamic" Schema) ---
    // This allows the waterfall or vitals to change without a migration.
    [Column(TypeName = "jsonb")]
    public string Metadata { get; set; } = null!;

    [Column(TypeName = "jsonb")]
    public string Session { get; set; } = null!;

    [Column(TypeName = "jsonb")]
    public string Performance { get; set; } = null!;

    [Column(TypeName = "jsonb")]
    public string? Network { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Device { get; set; }

    // --- AUDIT ---
    public DateTime IngestedAt { get; set; }
}