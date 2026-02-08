using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Tenancy.Entities;
using Libs.Domain;

namespace Api.Application.Limits.Entities;

[Table("tenant_limits")]
public class TenantLimitEntity : TenantAwareEntity
{
    [Column("limit_key")]
    public LimitKey LimitKey { get; set; }

    [Column("period")]
    public LimitPeriod Period { get; set; } = LimitPeriod.Monthly;

    [Column("max_amount")]
    public int MaxAmount { get; set; }

    [Column("current_amount")]
    public int CurrentAmount { get; set; } = 0;

    [Column("last_reset_at")]
    public DateTime? LastResetAt { get; set; }
}
