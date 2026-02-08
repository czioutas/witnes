
using System.ComponentModel.DataAnnotations;
using Api.Application.Authentication;
using Api.Application.Tenancy.Models;

namespace Api.Application.Users.Models;

public sealed record UserInvitationModel : TenantAwareModel
{
    [Required]
    public required string Email { get; set; }

    [Required]
    public bool Used { get; set; } = false;

    [Required]
    public required string InvitationToken { get; set; }

    [Required]
    public required string FirstName { get; set; }

    [Required]
    public required string LastName { get; set; }

    [Required]
    public required AccountRoles[] Roles { get; set; }
}
