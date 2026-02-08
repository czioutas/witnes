using Api.Application.Tenancy.Models;

namespace Api.Application.Account.Models;

public sealed record ApplicationUserModel : TenantAwareModel
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; } = string.Empty;
    public required string LastName { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
}
