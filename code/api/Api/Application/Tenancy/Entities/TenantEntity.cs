using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Account.Entities;
using Api.Application.Application.Entities;

namespace Api.Application.Tenancy.Entities;

[Table("tenants")]
public class TenantEntity : AuditableBaseEntity
{
    public string Identifier { get; set; } // the identifier can change but not the Id
    public string NormalizedIdentifier { get; set; } = string.Empty; // normalized identifier for URL-friendly lookups

    public ICollection<ApplicationUserEntity> ApplicationUsers { get; set; } = []; // Collection navigation containing dependents

    public TenantEntity()
    {
        Identifier = string.Empty;
    }

    public TenantEntity(string identifier)
    {
        Identifier = identifier;
    }
}
