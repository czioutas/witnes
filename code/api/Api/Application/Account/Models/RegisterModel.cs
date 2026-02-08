
using System.ComponentModel.DataAnnotations;
using Libs.Domain;

namespace Api.Application.Account.Models;
/// <summary>
/// Contract used to transmit required data for registration
/// </summary>
public sealed record RegisterModel
{
    /// <summary>
    /// The Email of the User
    /// </summary>
    [Required]
    [EmailAddress]
    public required string Email { get; init; } = string.Empty;

    /// <summary>
    /// The First Name of the User
    /// </summary>
    [Required]
    public required string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// The Last Name of the User
    /// </summary>
    [Required]
    public required string LastName { get; init; } = string.Empty;

    /// <summary>
    /// The Email of the User
    /// </summary>
    [Required]
    [MinLength(8, ErrorMessage = "Your password length must be at least 8.")]
    [MaxLength(100, ErrorMessage = "Your password length must not exceed 100.")]
    public required string Password { get; init; } = string.Empty;

    /// <summary>
    /// The tenant identifier that the user is registered with.
    /// </summary>
    [Required]
    public required string NormalizedTenantIdentifier { get; init; } = string.Empty;

    /// <summary>
    /// Invitation token for users invited by existing tenants (optional)
    /// </summary>
    public string? InvitationToken { get; init; } = default;
}
