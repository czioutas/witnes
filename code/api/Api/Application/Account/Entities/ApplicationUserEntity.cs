using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Api.Application.Tenancy.Entities;
using Microsoft.AspNetCore.Identity;

namespace Api.Application.Account.Entities;

[Table("AspNetUsers")]
public class ApplicationUserEntity : IdentityUser<Guid>
{
    [ForeignKey(nameof(Tenant))]
    public Guid TenantId { get; set; }
    public TenantEntity? Tenant { get; set; }
    public string? OneTimeLoginToken { get; set; }
    public DateTimeOffset? OneTimeLoginTokenCreatedAt { get; set; } = null;
    public string? RefreshToken { get; set; }
    public string? AccessToken { get; set; }
    public DateTimeOffset? AccessTokenCreatedAt { get; set; } = null;
    public DateTimeOffset? RefreshTokenCreatedAt { get; set; } = null;

    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public bool Disabled { get; set; } = false;
    public bool Limited { get; set; } = false;

    public ApplicationUserEntity()
    { }

    [SetsRequiredMembers]
    public ApplicationUserEntity(
            Guid tenantId,
            string email,
            string firstName,
            string lastName) : base(email)
    {
        TenantId = tenantId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }
}
