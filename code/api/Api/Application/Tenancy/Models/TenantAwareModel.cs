using Api.Application.Models;

namespace Api.Application.Tenancy.Models;

public record TenantAwareModel : BaseModel
{
    public Guid TenantId { get; set; }
}
