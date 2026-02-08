
using System.ComponentModel.DataAnnotations;
using Api.Application.Authentication;

namespace Api.Application.Users.Models;

public sealed record CreateUserInvitationModel
{
    [Required]
    public required string Email { get; set; }

    [Required]
    public required string FirstName { get; set; }

    [Required]
    public required string LastName { get; set; }

    [Required]
    public required AccountRoles[] Roles { get; set; }
}
