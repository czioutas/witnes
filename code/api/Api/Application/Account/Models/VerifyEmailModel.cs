using System.ComponentModel.DataAnnotations;

namespace Api.Application.Account.Models;

public sealed record VerifyEmailModel
{
    [Required]
    public required string Code { get; set; }

    [Required]
    public required Guid UserId { get; set; }
}
