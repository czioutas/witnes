using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Application.Entities;
using Libs.Domain;

namespace Api.Application.Features.Entities;

[Table("features")]
public class FeatureEntity : AuditableBaseEntity
{
    [Column("key")]
    public FeatureKey Key { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_global")]
    public bool IsGlobal { get; set; } = false;

    [Column("is_enabled_by_default")]
    public bool IsEnabledByDefault { get; set; } = false;

    public ICollection<TenantFeatureEntity> FeatureTenants { get; set; } = new List<TenantFeatureEntity>();
}
