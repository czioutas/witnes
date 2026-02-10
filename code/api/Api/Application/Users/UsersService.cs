using Api.Application;
using Api.Application.Account.Entities;
using Api.Application.Account.Models;
using Api.Application.Authentication;
using Api.Application.Communication.Services;
using Api.Application.Tenancy.Services;
using Api.Application.Users;
using Api.Application.Users.Models;
using Api.Data;
using Api.Settings.Settings;
using AutoMapper;
using Libs.Result;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Usersr.API.Users.Services;

public interface IUsersService
{
    Task<List<SlimApplicationUserModel>> GetAllAsync(Guid tenantId);
    Task<List<UserInvitationModel>> GetPendingUserInvitationsAsync();
    Task<Result<SlimApplicationUserModel>> GetAsync(Guid userId, Guid tenantId);
    Task<Result<UserInvitationModel>> InviteUserAsync(CreateUserInvitationModel inviteUserModel, Guid invitedByUserId);
    Task<Result<SlimApplicationUserModel>> UpdateUserAsync(UpdateUserModel updateUserModel);
    Task<Result<bool>> ToggleDisableAsync(Guid usersId, Guid tenantId);
    Task<Result<UserInvitationModel>> GetUserInvitationIfValidAsync(string token);
    Task<Result<bool>> MarkInvitationAsUsedAsync(Guid id);
    Task<Result<bool>> DeleteUserAsync(Guid userId, Guid tenantId, Guid requestingUserId);
    Task<Result<bool>> DeleteUserInvitationAsync(Guid invitationId, Guid tenantId);
    Task<string?> GetTenantAdminEmailAsync();
}

public class UsersService : IUsersService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UsersService> _logger;
    private readonly IMapper _mapper;
    private readonly ICommunicationService _communicationService;
    private readonly UserManager<ApplicationUserEntity> _userManager;
    private readonly TimeProvider _timeProvider;
    private readonly IRequestTenant _requestTenant;
    private readonly ITenantService _tenantService;
    private readonly ILinkResolver _linkResolver;
    private readonly SmtpSettings _smtpSettings;

    public UsersService(
        IMapper mapper,
        ApplicationDbContext dbContext,
        ILogger<UsersService> logger,
        ICommunicationService communicationService,
        UserManager<ApplicationUserEntity> userManager,
        TimeProvider timeProvider,
        IRequestTenant requestTenant,
        ITenantService tenantService,
        ILinkResolver linkResolver,
        IOptions<SmtpSettings> smtpSettings
    )
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _linkResolver = linkResolver ?? throw new ArgumentNullException(nameof(linkResolver));
        _smtpSettings = smtpSettings?.Value ?? throw new ArgumentNullException(nameof(smtpSettings));
    }

    // public async Task<Result<bool>> ApplyRolesAsync(ApplyRolesToUserModel applyRolesToUserModel)
    // {
    //     Result<ApplicationUserEntity> result = await FindUser(applyRolesToUserModel.ApplicationUserId, applyRolesToUserModel.TenantId);

    //     if (result.IsFailure)
    //     {
    //         return new Result<bool>();
    //     }

    //     var roleAssignResult = await _accountService.AssignRolesToUserAsync(applyRolesToUserModel.ApplicationUserId, applyRolesToUserModel.Roles);

    //     if (roleAssignResult.IsFailure)
    //     {
    //         return new Result<bool>(roleAssignResult.GetException);
    //     }

    //     return new Result<bool>(true);
    // }

    public async Task<Result<bool>> ToggleDisableAsync(Guid usersId, Guid tenantId)
    {
        var result = await FindUser(usersId, tenantId);

        if (result.IsFailure)
        {
            return Result<bool>.NotFound("User", usersId.ToString());
        }

        var entity = result.GetValue;
        entity.Disabled = !entity.Disabled;

        _dbContext.Update(entity);

        if (_dbContext.SaveChanges()! > 0)
        {
            return Result<bool>.BusinessError("DisableUser", "Disable failed. No rows affected.");
        }

        return new Result<bool>(true);
    }

    public async Task<List<SlimApplicationUserModel>> GetAllAsync(Guid tenantId)
    {
        // var slimUsers = _mapper.Map<List<SlimApplicationUserModel>>(await _dbContext.Users.Where(u => u.TenantId == tenantId).ToListAsync());

        return await _dbContext.Users
        .Include(u => u.Tenant)
        .Where(u => u.TenantId == tenantId)
        .Select(user => new SlimApplicationUserModel
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            TenantName = user.Tenant!.Identifier,
            Disabled = user.Disabled,
            Roles = _dbContext.UserRoles.Where(ur => ur.UserId == user.Id)
            .Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => Enum.Parse<AccountRoles>(r.Name!))
                .ToArray()
        })
        .ToListAsync();
    }

    public async Task<Result<UserInvitationModel>> InviteUserAsync(CreateUserInvitationModel inviteUserModel, Guid invitedByUserId)
    {
        var tenantId = _requestTenant.TenantId;
        var userResult = await FindUserAsync(inviteUserModel.Email, tenantId);

        if (!userResult.IsFailure)
        {
            return Result<UserInvitationModel>.BusinessError("UserExists", "User is already registed.");
        }

        var userInvitationResult = await FindUserInvitationAsync(inviteUserModel.Email, tenantId);

        if (!userInvitationResult.IsFailure)
        {
            return Result<UserInvitationModel>.BusinessError("InviteExists", "User has a pending invite.");
        }

        // Get the inviter user details
        var inviter = await _userManager.FindByIdAsync(invitedByUserId.ToString());
        string inviterName = "A team member";
        if (inviter != null)
        {
            inviterName = $"{inviter.FirstName} {inviter.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(inviterName))
            {
                inviterName = inviter.Email ?? "A team member";
            }
        }

        var userInvitationEntity = _mapper.Map<UserInvitationEntity>(inviteUserModel);
        userInvitationEntity.ExpiresOn = _timeProvider.GetUtcNow().AddDays(7);
        userInvitationEntity.InvitationToken = ShuffleString(Guid.NewGuid().ToString("N"));
        userInvitationEntity.InvitedByUserId = invitedByUserId;
        await _dbContext.UserInvitations.AddAsync(userInvitationEntity);
        var dbResult = _dbContext.SaveChanges();

        if (dbResult != 1)
        {
            return Result<UserInvitationModel>.BusinessError("DatabaseError", "Unexpected number of rows affected: " + dbResult);
        }

        // Try to send invitation email, but don't fail the request if it fails
        try
        {
            var emailSent = await SendInvitationEmailAsync(inviteUserModel, tenantId, userInvitationEntity.InvitationToken, inviterName);
            if (!emailSent)
            {
                _logger.LogWarning("Failed to send invitation email to {Email} for tenant {TenantId}. Invitation created but email not sent.", inviteUserModel.Email, tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending invitation email to {Email} for tenant {TenantId}. Invitation created but email not sent.", inviteUserModel.Email, tenantId);
        }

        return new Result<UserInvitationModel>(_mapper.Map<UserInvitationModel>(userInvitationEntity));
    }

    private async Task<bool> SendInvitationEmailAsync(CreateUserInvitationModel invitationModel, Guid tenantId, string invitationCode, string inviterName)
    {
        var tenantResult = await _tenantService.GetAsync(tenantId);

        // Remove problematic characters that get HTML encoded by Mailtrap
        var companyName = tenantResult.GetValue.Identifier
            .Replace("'", "")
            .Replace("\"", "")
            .Replace("<", "")
            .Replace(">", "")
            .Replace("&", "and");

        var sanitizedInviterName = inviterName
            .Replace("'", "")
            .Replace("\"", "")
            .Replace("<", "")
            .Replace(">", "")
            .Replace("&", "and");

        var url = _linkResolver.GetApplicationUrl("authenticate/register-invitation", ("invitationCode", invitationCode), ("email", invitationModel.Email), ("firstName", invitationModel.FirstName), ("lastName", invitationModel.LastName), ("company", companyName));

        return await _communicationService.SendEmailAsync(
            _smtpSettings.UserInvitationTemplateId,
            invitationModel.Email,
            new { firstName = invitationModel.FirstName, link = url, company = companyName, inviterName = sanitizedInviterName });
    }

    public async Task<Result<SlimApplicationUserModel>> UpdateUserAsync(UpdateUserModel updateUserModel)
    {
        ApplicationUserEntity? userEntity = await _dbContext.Users.FirstOrDefaultAsync(m => m.Id == updateUserModel.UserId && m.TenantId == _requestTenant.TenantId);

        if (userEntity is null)
        {
            return Result<SlimApplicationUserModel>.NotFound("User", updateUserModel.UserId.ToString());
        }

        userEntity.LastName = string.IsNullOrEmpty(updateUserModel.LastName) ? userEntity.LastName : updateUserModel.LastName;
        userEntity.FirstName = string.IsNullOrEmpty(updateUserModel.FirstName) ? userEntity.FirstName : updateUserModel.FirstName;
        userEntity.Email = string.IsNullOrEmpty(updateUserModel.Email) ? userEntity.Email : updateUserModel.Email;

        var existingUserRoles = await _userManager.GetRolesAsync(userEntity);
        var incomingRolesInString = updateUserModel.Roles.Select(role => role.ToString()).ToList();

        // Check if we're removing admin role from a user who currently has it
        bool wasAdmin = existingUserRoles.Contains(AccountRoles.AdminUserRole.ToString());
        bool willBeAdmin = incomingRolesInString.Contains(AccountRoles.AdminUserRole.ToString());

        // If removing admin role, ensure at least one admin remains
        if (wasAdmin && !willBeAdmin)
        {
            var guardResult = await EnsureAtLeastOneAdminRemainsAsync(_requestTenant.TenantId, updateUserModel.UserId);
            if (guardResult.IsFailure)
            {
                return Result<SlimApplicationUserModel>.Failure(guardResult.ErrorModel);
            }
        }

        // first we remove any roles that dont exist in the incoming update Model
        if (existingUserRoles is not null && existingUserRoles.Any() && existingUserRoles.Any(r => !incomingRolesInString.Contains(r)))
        {
            await _userManager.RemoveFromRolesAsync(userEntity, existingUserRoles.Where(r => !incomingRolesInString.Contains(r)).ToList());
        }

        // then we add any roles that are in the update Model but not in the existing
        if (existingUserRoles is not null && incomingRolesInString.Any(r => !existingUserRoles.Contains(r)))
        {
            await _userManager.AddToRolesAsync(userEntity, incomingRolesInString.Where(r => !existingUserRoles.Contains(r)).ToList());
        }

        _dbContext.Update(userEntity);
        _dbContext.SaveChanges();


        SlimApplicationUserModel userModel = _mapper.Map<SlimApplicationUserModel>(userEntity);

        userModel.Roles = (await _userManager.GetRolesAsync(userEntity))
            .Select(role => Enum.Parse<AccountRoles>(role, ignoreCase: true))
            .ToArray();

        return new Result<SlimApplicationUserModel>(userModel);
    }

    private async Task<Result<ApplicationUserEntity>> FindUser(Guid userId, Guid tenantId)
    {
        ApplicationUserEntity? user = await _dbContext.Users.FirstOrDefaultAsync(m => m.Id == userId && m.TenantId == tenantId);

        if (user is null)
        {
            return Result<ApplicationUserEntity>.NotFound("User", userId.ToString());
        }

        return new Result<ApplicationUserEntity>(user);
    }

    private async Task<Result<ApplicationUserEntity>> FindUserAsync(string email, Guid tenantId)
    {
        ApplicationUserEntity? user = await _dbContext.Users.FirstOrDefaultAsync(m => m.Email == email && m.TenantId == tenantId);

        if (user is null)
        {
            return Result<ApplicationUserEntity>.NotFound("User", email);
        }

        return new Result<ApplicationUserEntity>(user);
    }

    private async Task<Result<UserInvitationEntity>> FindUserInvitationAsync(string email, Guid tenantId)
    {
        UserInvitationEntity? userInvitation = await _dbContext.UserInvitations.FirstOrDefaultAsync(m => m.Email == email && m.TenantId == tenantId);

        if (userInvitation is null)
        {
            return Result<UserInvitationEntity>.NotFound("UserInvitation", email);
        }

        return new Result<UserInvitationEntity>(userInvitation);
    }

    public async Task<Result<UserInvitationModel>> GetUserInvitationIfValidAsync(string token)
    {
        // Invitations when used from the person who wants to register cannot be found if we dont have the ignore filter
        UserInvitationEntity? userInvitationEntity = await _dbContext.UserInvitations.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.InvitationToken == token);

        if (userInvitationEntity == null)
        {
            return Result<UserInvitationModel>.NotFound("UserInvitation", token);
        }

        if (userInvitationEntity.Used)
        {
            return Result<UserInvitationModel>.BusinessError("InviteUsed", "Invitation has already been used.");
        }

        if (_timeProvider.GetUtcNow() > userInvitationEntity.ExpiresOn)
        {
            return Result<UserInvitationModel>.BusinessError("InviteExpired", "Invitation has expired.");
        }

        return new Result<UserInvitationModel>(_mapper.Map<UserInvitationModel>(userInvitationEntity));
    }

    public static string ShuffleString(string input)
    {
        Random rng = new Random();

        // Convert the string to a char array
        char[] array = input.ToCharArray();

        // Fisher-Yates shuffle algorithm
        int n = array.Length;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            char value = array[k];
            array[k] = array[n];
            array[n] = value;
        }

        // Convert the char array back to a string
        return new string(array);
    }

    public async Task<List<UserInvitationModel>> GetPendingUserInvitationsAsync()
    {
        return _mapper.Map<List<UserInvitationModel>>(await _dbContext.UserInvitations.Where(ui => ui.Used == false).ToListAsync());
    }

    public async Task<Result<SlimApplicationUserModel>> GetAsync(Guid userId, Guid tenantId)
    {
        var userResult = await FindUser(userId, tenantId);
        if (userResult.IsFailure)
        {
            return Result<SlimApplicationUserModel>.Failure(userResult.ErrorModel);
        }

        return new Result<SlimApplicationUserModel>(_mapper.Map<SlimApplicationUserModel>(userResult.GetValue));
    }

    public async Task<Result<bool>> MarkInvitationAsUsedAsync(Guid id)
    {
        UserInvitationEntity? userInvitation = await _dbContext.UserInvitations.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == id);

        if (userInvitation is null)
        {
            return Result<bool>.NotFound("UserInvitation", id.ToString());
        }

        userInvitation.Used = true;
        _dbContext.Update(userInvitation);
        await _dbContext.SaveChangesAsync();

        return new Result<bool>(true);
    }

    /// <summary>
    /// Ensures that at least one admin user will remain in the tenant after the operation
    /// </summary>
    private async Task<Result<bool>> EnsureAtLeastOneAdminRemainsAsync(Guid tenantId, Guid? userIdToExclude = null)
    {
        // Get all users in the tenant with their roles
        var adminUsers = await _dbContext.Users
            .Where(u => u.TenantId == tenantId && (userIdToExclude == null || u.Id != userIdToExclude))
            .Where(u => _dbContext.UserRoles
                .Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Any(ur => ur.UserId == u.Id && ur.Name == AccountRoles.AdminUserRole.ToString()))
            .CountAsync();

        if (adminUsers < 1)
        {
            return Result<bool>.BusinessError("LastAdmin", "Cannot perform this operation. At least one admin user must remain in the organization.");
        }

        return new Result<bool>(true);
    }

    public async Task<Result<bool>> DeleteUserAsync(Guid userId, Guid tenantId, Guid requestingUserId)
    {
        // Prevent self-deletion
        if (userId == requestingUserId)
        {
            return Result<bool>.BusinessError("SelfDeletion", "You cannot delete your own account.");
        }

        var userResult = await FindUser(userId, tenantId);
        if (userResult.IsFailure)
        {
            return Result<bool>.NotFound("User", userId.ToString());
        }

        var userEntity = userResult.GetValue;

        // Check if user is an admin
        var userRoles = await _userManager.GetRolesAsync(userEntity);
        var isAdmin = userRoles.Contains(AccountRoles.AdminUserRole.ToString());

        // If deleting an admin, ensure at least one admin remains
        if (isAdmin)
        {
            var guardResult = await EnsureAtLeastOneAdminRemainsAsync(tenantId, userId);
            if (guardResult.IsFailure)
            {
                return guardResult;
            }
        }

        // Delete the user
        var deleteResult = await _userManager.DeleteAsync(userEntity);
        if (!deleteResult.Succeeded)
        {
            return Result<bool>.BusinessError("DeleteFailed", $"Failed to delete user: {string.Join(", ", deleteResult.Errors.Select(e => e.Description))}");
        }

        return new Result<bool>(true);
    }

    public async Task<string?> GetTenantAdminEmailAsync()
    {
        var adminEmail = await _dbContext.Users
            .Where(u => _dbContext.UserRoles
                .Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Any(ur => ur.UserId == u.Id && ur.Name == AccountRoles.AdminUserRole.ToString()))
            .Select(u => u.Email)
            .FirstOrDefaultAsync();

        return adminEmail;
    }

    public async Task<Result<bool>> DeleteUserInvitationAsync(Guid invitationId, Guid tenantId)
    {
        UserInvitationEntity? invitation = await _dbContext.UserInvitations
            .FirstOrDefaultAsync(m => m.Id == invitationId && m.TenantId == tenantId);

        if (invitation is null)
        {
            return Result<bool>.NotFound("UserInvitation", invitationId.ToString());
        }

        _dbContext.UserInvitations.Remove(invitation);
        await _dbContext.SaveChangesAsync();

        return new Result<bool>(true);
    }
}
