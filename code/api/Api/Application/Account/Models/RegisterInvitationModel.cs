using System.ComponentModel.DataAnnotations;

namespace Api.Application.Account.Models;

/// <summary>
/// Contract for completing registration from an invitation
/// </summary>
public sealed record RegisterInvitationModel
{
    /// <summary>
    /// The invitation code from the email
    /// </summary>
    [Required]
    public required string InvitationCode { get; init; }

    /// <summary>
    /// The password chosen by the invited user
    /// </summary>
    [Required]
    [MinLength(8, ErrorMessage = "Your password length must be at least 8.")]
    [MaxLength(100, ErrorMessage = "Your password length must not exceed 100.")]
    public required string Password { get; init; }
}
