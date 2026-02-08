using System.ComponentModel.DataAnnotations;

namespace Api.Application.Account.Models;

public sealed record ResetPasswordModel
{
    [Required]
    public required string Code { get; set; }

    [Required]
    public required string NewPassword { get; set; }

    [Required]
    public required Guid UserId { get; set; }
}
