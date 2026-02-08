using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Tenancy.Entities;

namespace Api.Application.Features.Entities;

[Table("tenant_features")]
public class TenantFeatureEntity : TenantAwareEntity
{
    [Column("feature_id")]
    public Guid FeatureId { get; set; }

    public FeatureEntity Feature { get; set; } = null!;

    [Column("is_enabled")]
    public bool IsEnabled { get; set; } = true;

    [Column("enabled_from")]
    public DateOnly? EnabledFrom { get; set; }

    [Column("enabled_until")]
    public DateOnly? EnabledUntil { get; set; }
}
