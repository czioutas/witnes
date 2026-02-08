using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Application.Entities;

namespace Api.Application.Tenancy.Entities;

/// <summary>
/// Tenant-aware entity with Guid ID (for backwards compatibility) - application generates GUIDs
/// </summary>
public class TenantAwareEntity : AuditableBaseEntity
{
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public TenantEntity? Tenant { get; set; }
}

/// <summary>
/// Tenant-aware entity with long ID (for high-volume entities like Activities) - database auto-generates IDs
/// </summary>
public class TenantAwareEntityLong : AuditableBaseEntityLong
{
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public TenantEntity? Tenant { get; set; }
}
