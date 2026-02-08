using System.ComponentModel.DataAnnotations;

namespace Api.Application.Account.Models;

public sealed record TokenModel
{
    [Required]
    public required string AccessToken { get; set; }

    [Required]
    public required string RefreshToken { get; set; }

    [Required]
    public required DateTimeOffset CreatedAt { get; set; }
}

