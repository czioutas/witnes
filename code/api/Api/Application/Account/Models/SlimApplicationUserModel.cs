using System.ComponentModel.DataAnnotations;
using Api.Application.Authentication;

namespace Api.Application.Account.Models;

public sealed record SlimApplicationUserModel
{
    [Required]
    public required Guid Id { get; set; }

    [Required]
    public required string Email { get; set; }

    [Required]
    public required string FirstName { get; set; }

    [Required]
    public required string LastName { get; set; }

    [Required]
    public required string TenantName { get; set; }

    public bool Disabled { get; set; }

    public bool Limited { get; set; }

    [Required]
    public required AccountRoles[]? Roles { get; set; }
}
