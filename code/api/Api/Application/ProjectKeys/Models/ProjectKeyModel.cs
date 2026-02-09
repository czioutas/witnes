namespace Api.Application.ProjectKeys.Models;

public class ProjectKeyModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ProjectKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public required string Domain { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
