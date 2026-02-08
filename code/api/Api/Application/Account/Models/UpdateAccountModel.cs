namespace Api.Application.Account.Models;

public sealed record UpdateAccountModel
{
    /// <summary>
    /// The First Name of the User
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// The Last Name of the User
    /// </summary>
    public string LastName { get; init; } = string.Empty;
}
