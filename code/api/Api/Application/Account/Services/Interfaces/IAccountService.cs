using Api.Application.Account.Entities;
using Api.Application.Account.Models;
using Api.Application.Authentication;
using Libs.Result;
using Microsoft.AspNetCore.Identity;

namespace Api.Application.Account.Services.Interfaces;

public interface IAccountService
{
    Task<IList<string>> GetUserRolesAsync(Guid userId);
    Task<Result<ApplicationUserModel>> RegisterAsync(RegisterModel registerModel, AccountRoles[]? roles, bool bypassTenantCheck = false);
    Task<Result<TokenModel>> LoginAsync(string email, string password);
    Task<TokenModel> LoginAsync(Guid userId, string logintoken);
    Task SignOutAsync(Guid userId);
    Task<ApplicationUserEntity?> FindByEmailAsync(string email);
    Task<ApplicationUserEntity> GetUserOrThrowAsync(Guid id);
    Task<ApplicationUserEntity> GetUserAsync(string email);
    Task<Result<SlimApplicationUserModel>> GetSelfAsync(Guid userId);
    Task<TokenModel> GenerateJWTForUserAsync(Guid userId);
    Task<IdentityResult> ConfirmEmailAsync(Guid userId, string code);
    Task PatchAsync(Guid selfId, UpdateAccountModel patchUserModel);
    Task<Result<bool>> AssignRolesToUserAsync(Guid userId, AccountRoles[] roles);
    Task DeleteUserAsync(Guid id);
    Task<TokenModel> GetTokenForUser(string email);
    Task<TokenModel> GetTokenForUser(Guid id);
    Task<TokenModel> GetShortSpanTokenForUser(string email);
    Task SendVerificationEmailAsync(string firstName, Guid userId);
    Task<bool> VerifyEmailAsync(string code, Guid userId);
    Task RequestPasswordResetAsync(string userEmail);
    Task<bool> ResetPasswordAsync(string code, string newPassword, Guid userId);
    Task<Result<TokenModel>> RegisterFromInvitationAsync(RegisterInvitationModel registerInvitationModel);
}
