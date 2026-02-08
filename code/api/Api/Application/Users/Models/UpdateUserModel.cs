using System.ComponentModel.DataAnnotations;
using Api.Application.Authentication;

namespace Api.Application.Users.Models;

public sealed record UpdateUserModel
{
    [Required]
    public Guid UserId { get; set; }

    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public AccountRoles[] Roles { get; set; } = [];
}
