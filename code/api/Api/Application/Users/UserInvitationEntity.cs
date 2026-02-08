using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Tenancy.Entities;

namespace Api.Application.Users;

[Table("user_invitations")]
public class UserInvitationEntity : TenantAwareEntity
{
    public required string Email { get; set; }
    public bool Used { get; set; } = false;
    public required string InvitationToken { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string[] Roles { get; set; }
    public required DateTimeOffset ExpiresOn { get; set; }
    public Guid InvitedByUserId { get; set; }
}
