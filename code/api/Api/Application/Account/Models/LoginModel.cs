using System.ComponentModel.DataAnnotations;

namespace Api.Application.Account.Models;

public sealed record LoginModel
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }
}
