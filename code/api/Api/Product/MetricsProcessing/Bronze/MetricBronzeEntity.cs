using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Tenancy.Entities;
using Api.Product.Ingestion.Models;

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
    // Npgsql auto-serializes these POCOs to/from jsonb via EnableDynamicJson().
    [Column(TypeName = "jsonb")]
    public MetadataModel Metadata { get; set; } = null!;

    [Column(TypeName = "jsonb")]
    public SessionModel Session { get; set; } = null!;

    [Column(TypeName = "jsonb")]
    public PerformanceModel Performance { get; set; } = null!;

    [Column(TypeName = "jsonb")]
    public NetworkModel? Network { get; set; }

    [Column(TypeName = "jsonb")]
    public DeviceModel? Device { get; set; }

    // --- AUDIT ---
    public DateTimeOffset IngestedAt { get; set; }
    public DateTimeOffset PageRequestedAtByVisitor { get; set; }
}