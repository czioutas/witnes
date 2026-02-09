using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Tenancy.Entities;

namespace Api.Application.ProjectKeys.Entities;

[Table("project_keys")]
public class ProjectKeyEntity : TenantAwareEntity
{
    [Column("project_key")]
    [MaxLength(50)]
    public string ProjectKey { get; set; } = string.Empty;

    [Column("name")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Column("domain")]
    public required string Domain { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("last_used_at")]
    public DateTimeOffset? LastUsedAt { get; set; }
}
