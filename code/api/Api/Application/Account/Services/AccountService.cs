using Api.Application.Account.Entities;
using Api.Application.Account.Models;
using Api.Application.Account.Services.Interfaces;
using Api.Application.Authentication;
using Api.Application.Communication.Services;
using Api.Application.Pricing.Services;
using Api.Application.Tenancy.Models;
using Api.Application.Tenancy.Services;
using Api.Application.Users.Models;
using Api.Data;
using Api.Settings.Settings;
using AutoMapper;
using Libs.Exceptions;
using Libs.Result;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Usersr.API.Users.Services;

namespace Api.Application.Account.Services;

public class AccountService : IAccountService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUserEntity> _userManager;
    private readonly SignInManager<ApplicationUserEntity> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AccountService> _logger;
    private readonly ITenantService _tenantService;
    private readonly IUsersService _usersService;
    private readonly IMapper _mapper;
    private readonly ILinkResolver _linkResolver;
    private readonly ICommunicationService _communicationService;
    private readonly ITenantPricingService _tenantPricingService;
    private readonly IConfiguration _configuration;
    private readonly IRequestTenant _requestTenant;
    private readonly SmtpSettings _smtpSettings;

    public AccountService(
        ApplicationDbContext dbContext,
        ITenantService tenantService,
        IRequestTenant requestTenant,
        UserManager<ApplicationUserEntity> userManager,
        SignInManager<ApplicationUserEntity> signInManager,
        ITokenService tokenService,
        TimeProvider timeProvider,
        IMapper mapper,
        IUsersService usersService,
        ILogger<AccountService> logger,
        ILinkResolver linkResolver,
        ICommunicationService communicationService,
        ITenantPricingService tenantPricingService,
        IConfiguration configuration,
        IOptions<SmtpSettings> smtpSettings)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _linkResolver = linkResolver ?? throw new ArgumentNullException(nameof(linkResolver));
        _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));
        _tenantPricingService = tenantPricingService ?? throw new ArgumentNullException(nameof(tenantPricingService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _smtpSettings = smtpSettings?.Value ?? throw new ArgumentNullException(nameof(smtpSettings));
    }

    public async Task<Result<ApplicationUserModel>> RegisterAsync(RegisterModel registerModel, AccountRoles[]? roles, bool bypassTenantCheck = false)
    {
        // if there is no registed user 
        // if there is no registered tenant
        Result<ApplicationUserEntity>? userRegisterResult = null;
        AccountRoles[] _roles = [];

        var userDoesntExistResult = await UserDoesntExist(registerModel);

        if (userDoesntExistResult.IsFailure)
        {
            return Result<ApplicationUserModel>.Failure(userDoesntExistResult.ErrorModel);
        }

        // if (!string.IsNullOrEmpty(registerModel.InvitationToken))
        // {
        //     var tenantModelResult = await _tenantService.GetAsync(registerModel.TenantIdentifier);
        //     if (tenantModelResult.IsFailure)
        //     {
        //         return new Result<ApplicationUserModel>(tenantModelResult.GetException);
        //     }
        // }

        // from now on we know that its a valid registration

        if (!string.IsNullOrEmpty(registerModel.InvitationToken))
        {
            var userInvitationResult = await HandleInvitedRegistrationAsync(registerModel);

            if (userInvitationResult.IsFailure)
            {
                return Result<ApplicationUserModel>.Failure(userInvitationResult.ErrorModel);
            }

            _roles = userInvitationResult.GetValue.Roles;

            userRegisterResult = await CreateUserAsync(registerModel, _requestTenant.TenantId, true);
        }
        else
        {
            var result = await HandleSelfRegistrationAsync(registerModel, bypassTenantCheck);

            if (result.IsFailure)
            {
                return result.ToErrorResult<ApplicationUserModel>();
            }

            _roles = roles ?? [AccountRoles.AdminUserRole];
            userRegisterResult = await CreateUserAsync(registerModel, result.GetValue.Id);
        }

        if (userRegisterResult.IsFailure)
        {
            return Result<ApplicationUserModel>.Failure(userRegisterResult.ErrorModel);
        }

        var roleAssignResult = await AssignRolesForNewUserAsync(userRegisterResult.GetValue, _roles);

        if (roleAssignResult.IsFailure)
        {
            return Result<ApplicationUserModel>.BusinessError("RoleAssignment", "Could not create roles for user.");
        }

        return new Result<ApplicationUserModel>(_mapper.Map<ApplicationUserModel>(userRegisterResult.GetValue));
    }

    private async Task<Result<ApplicationUserModel>> AssignRolesForNewUserAsync(ApplicationUserEntity userEntity, AccountRoles[] roles)
    {
        var roleAssingResult = await _userManager.AddToRolesAsync(userEntity, roles.Select(role => role.ToString()).ToArray());

        if (!roleAssingResult.Succeeded)
        {
            return Result<ApplicationUserModel>.BusinessError("RoleAssignment", "Could not assign role to user.");
        }

        return new Result<ApplicationUserModel>(_mapper.Map<ApplicationUserModel>(userEntity));
    }

    private async Task<Result<UserInvitationModel>> HandleInvitedRegistrationAsync(RegisterModel registerModel)
    {
        if (string.IsNullOrEmpty(registerModel.InvitationToken))
        {
            return Result<UserInvitationModel>.Failure(new ResourceNotFoundErrorModel("Invalid user invitation token.", nameof(UserInvitationModel)));
        }

        var userInvitationResult = await _usersService.GetUserInvitationIfValidAsync(registerModel.InvitationToken);

        if (userInvitationResult.IsFailure)
        {
            return Result<UserInvitationModel>.Failure(new ResourceNotFoundErrorModel("Invalid user invitation.", nameof(UserInvitationModel)));
        }

        await _usersService.MarkInvitationAsUsedAsync(userInvitationResult.GetValue.Id);

        return new Result<UserInvitationModel>(userInvitationResult.GetValue);
    }

    private async Task<Result<TenantModel>> HandleSelfRegistrationAsync(RegisterModel registerModel, bool bypassTenantCheck)
    {
        string tenantIdentifier = registerModel.NormalizedTenantIdentifier ?? _tenantService.GetRandomIdentifier();

        var tenant = await _tenantService.GetAsync(tenantIdentifier);

        if (tenant.IsFailure)
        {
            var tenantResult = await _tenantService.CreateAsync(tenantIdentifier);

            if (!tenantResult.IsFailure)
            {
                // Set Starter pricing tier for newly created tenant
                var starterTier = await _tenantPricingService.GetStarterTierAsync();
                if (starterTier != null)
                {
                    _requestTenant.SetTenantId(tenantResult.GetValue.Id);
                    var pricingResult = await _tenantPricingService.SetTenantPricingAsync(starterTier.Id, isTrial: true);
                    if (pricingResult.IsFailure)
                    {
                        _logger.LogWarning("Failed to set Starter pricing tier for tenant {TenantId}: {Error}",
                            tenantResult.GetValue.Id, pricingResult.ErrorModel?.Message);
                    }
                }
                else
                {
                    _logger.LogWarning("Starter pricing tier not found for tenant {TenantId}", tenantResult.GetValue.Id);
                }
            }

            return tenantResult;
        }



        if (bypassTenantCheck)
        {
            return tenant;
        }
        else
        {
            return Result<TenantModel>.BusinessError("TenantAccess", "You cannot join this Tenant.");
        }
    }

    private async Task<Result<bool>> UserDoesntExist(RegisterModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user != null)
        {
            return Result<bool>.BusinessError("UserExists", "User already exists");
        }

        return new Result<bool>(true);
    }

    private async Task<Result<ApplicationUserEntity>> CreateUserAsync(RegisterModel registerModel, Guid tenantId, bool verified = false)
    {
        var bypassEmailVerification = _configuration.GetValue<bool>("IntegrationTestSettings:BypassEmailVerification", false);
        var shouldVerify = verified || bypassEmailVerification;

        var userEntity = new ApplicationUserEntity(tenantId: tenantId, email: registerModel.Email, firstName: registerModel.FirstName, lastName: registerModel.LastName)
        {
            EmailConfirmed = shouldVerify,
            CreatedAt = _timeProvider.GetUtcNow()
        };

        var result = await _userManager.CreateAsync(userEntity, registerModel.Password);

        if (!result.Succeeded)
        {
            return Result<ApplicationUserEntity>.ValidationError("UserCreation", $"User creation failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return new Result<ApplicationUserEntity>(userEntity);
    }

    public async Task<ApplicationUserEntity?> FindByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<ApplicationUserEntity> GetUserOrThrowAsync(Guid id)
    {
        return await _userManager.FindByIdAsync(id.ToString()) ?? throw new ResourceNotFoundException();
    }

    public async Task<ApplicationUserEntity> GetUserAsync(string email)
    {
        var a = await _userManager.FindByEmailAsync(email) ?? throw new ResourceNotFoundException();
        return a;
    }

    public async Task SignOutAsync(Guid userId)
    {
        ApplicationUserEntity userEntity = await GetUserOrThrowAsync(userId);

        userEntity.RefreshToken = string.Empty;
        await _userManager.UpdateAsync(userEntity);
    }

    public async Task<TokenModel> GenerateJWTForUserAsync(Guid userId)
    {
        ApplicationUserEntity userEntity = await GetUserOrThrowAsync(userId);

        var roles = await _userManager.GetRolesAsync(userEntity);
        var userModel = _mapper.Map<ApplicationUserModel>(userEntity);
        TokenModel tokens = _tokenService.GenerateToken(userModel, roles, null);

        userEntity.RefreshToken = tokens.RefreshToken;
        userEntity.RefreshTokenCreatedAt = _timeProvider.GetUtcNow();
        userEntity.AccessToken = tokens.AccessToken;
        userEntity.AccessTokenCreatedAt = _timeProvider.GetUtcNow();
        await _userManager.UpdateAsync(userEntity);

        return tokens;
    }

    public async Task<IdentityResult> ConfirmEmailAsync(Guid userId, string code)
    {
        var user = await GetUserOrThrowAsync(userId);
        return await _userManager.ConfirmEmailAsync(user, code);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email) ?? throw new ResourceNotFoundException();
        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<IdentityResult> ResetPasswordAsync(Guid userId, string token, string password)
    {
        var user = await GetUserOrThrowAsync(userId);
        return await _userManager.ResetPasswordAsync(user, token, password);
    }

    public async Task<Result<TokenModel>> LoginAsync(string email, string password)
    {
        ApplicationUserEntity? user = await _userManager.FindByEmailAsync(email);

        if (user is null || user.UserName is null)
        {
            return Result<TokenModel>.BusinessError("Authentication", "Invalid credentials");
        }

        if (!user.EmailConfirmed)
        {
            return Result<TokenModel>.BusinessError("Authentication", "Email not verified");
        }

        SignInResult signInResult = await _signInManager.PasswordSignInAsync(user, password, false, false);

        if (!signInResult.Succeeded)
        {
            return Result<TokenModel>.BusinessError("Authentication", "Invalid credentials");
        }

        var token = await GenerateJWTForUserAsync(user.Id);
        return new Result<TokenModel>(token);
    }

    /// <summary>
    /// Checks
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="loginToken"></param>
    /// <returns></returns>
    public async Task<TokenModel> LoginAsync(Guid userId, string loginToken)
    {
        ApplicationUserEntity user = await GetUserOrThrowAsync(userId);
        DateTime currentTime = _timeProvider.GetUtcNow().DateTime;

        if (
            user.OneTimeLoginToken != loginToken ||
            user.OneTimeLoginTokenCreatedAt.HasValue == false ||
            currentTime > user.OneTimeLoginTokenCreatedAt.Value.AddMinutes(5).ToUniversalTime().DateTime // TODO Move to config file
        )
        {
            throw new ResourceNotFoundException();
        }

        user.OneTimeLoginToken = string.Empty;
        user.OneTimeLoginTokenCreatedAt = null;

        await _userManager.UpdateAsync(user);

        return await GenerateJWTForUserAsync(userId);
    }

    public async Task PatchAsync(Guid selfId, UpdateAccountModel patchUserModel)
    {
        ApplicationUserEntity? applicationUserEntity = await _userManager.FindByIdAsync(selfId.ToString()) ?? throw new ResourceNotFoundException();

        applicationUserEntity.LastName = string.IsNullOrEmpty(patchUserModel.LastName) ? applicationUserEntity.LastName : patchUserModel.LastName;
        applicationUserEntity.FirstName = string.IsNullOrEmpty(patchUserModel.FirstName) ? applicationUserEntity.FirstName : patchUserModel.FirstName;

        IdentityResult result = await _userManager.UpdateAsync(applicationUserEntity);

        if (!result.Succeeded)
        {
            throw new IdentityException(result.Errors);
        }
    }

    public async Task<Result<bool>> AssignRolesToUserAsync(Guid userId, AccountRoles[] roles)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
        {
            return Result<bool>.NotFound("User", userId.ToString());
        }

        var result = await _userManager.AddToRolesAsync(user, roles.Select(role => role.ToString()).ToArray());

        if (!result.Succeeded)
        {
            return Result<bool>.BusinessError("RoleAssignment", "Could not allocate role to user");
        }

        return new Result<bool>(true);
    }

    public async Task<IList<string>> GetUserRolesAsync(Guid userId)
    {
        var userEntity = await _userManager.FindByIdAsync(userId.ToString());

        if (userEntity == null)
        {
            throw new ResourceNotFoundException();
        }

        return await _userManager.GetRolesAsync(userEntity);
    }

    public async Task<TokenModel> GetTokenForUser(Guid userId)
    {
        var roles = await GetUserRolesAsync(userId);
        var user = await GetUserOrThrowAsync(userId);
        var userModel = _mapper.Map<ApplicationUserModel>(user);
        return _tokenService.GenerateToken(userModel, roles);
    }

    public async Task<TokenModel> GetTokenForUser(string email)
    {
        var user = await GetUserAsync(email);
        var roles = await GetUserRolesAsync(user.Id);
        var userModel = _mapper.Map<ApplicationUserModel>(user);
        return _tokenService.GenerateToken(userModel, roles);
    }

    public async Task<TokenModel> GetShortSpanTokenForUser(string email)
    {
        var user = await GetUserAsync(email);
        var roles = await GetUserRolesAsync(user.Id);
        var userModel = _mapper.Map<ApplicationUserModel>(user);
        return _tokenService.GenerateShortSpanToken(userModel, roles);
    }

    public async Task DeleteUserAsync(Guid id)
    {
        var userEntity = await _userManager.FindByIdAsync(id.ToString());

        if (userEntity == null)
        {
            throw new ResourceNotFoundException();
        }

        await _userManager.DeleteAsync(userEntity);
    }

    public async Task<Result<SlimApplicationUserModel>> GetSelfAsync(Guid userId)
    {
        var userEntity = await _dbContext.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (userEntity is null)
        {
            return Result<SlimApplicationUserModel>.NotFound("User", userId.ToString());
        }

        // Get user roles
        var roles = await _userManager.GetRolesAsync(userEntity);
        var accountRoles = roles.Select(r => Enum.Parse<AccountRoles>(r)).ToArray();

        var userModel = _mapper.Map<SlimApplicationUserModel>(userEntity);
        userModel.Roles = accountRoles;

        return new Result<SlimApplicationUserModel>(userModel);
    }


    public async Task SendVerificationEmailAsync(string firstName, Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            throw new ResourceNotFoundException("No such user", userId);
        }

        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var url = _linkResolver.GetApplicationUrl("authenticate/verify-email", ("code", code), ("userId", user.Id.ToString()));
        _logger.LogInformation("Verification email URL: {Url}", url);

        await _communicationService.SendEmailAsync(_smtpSettings.VerifyEmailTemplateId, user.Email!, new { firstName, link = url });
    }

    public async Task<bool> VerifyEmailAsync(string code, Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
        {
            throw new ResourceNotFoundException("No such user", userId);
        }

        IdentityResult result = await _userManager.ConfirmEmailAsync(user, code);

        return result.Succeeded;
    }

    public async Task RequestPasswordResetAsync(string userEmail)
    {
        var user = await _userManager.FindByEmailAsync(userEmail);

        if (user is not null)
        {
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var url = _linkResolver.GetApplicationUrl("authenticate/reset-password", ("code", code), ("userId", user.Id.ToString()));
            _logger.LogInformation("Password reset URL: {Url}", url);

            await _communicationService.SendEmailAsync(_smtpSettings.PasswordResetTemplateId, user.Email!, new { link = url });
        }
        else
        {
            _logger.LogWarning("Forgot password attempt for non existing password: {UserEmail}", userEmail);
        }
    }

    public async Task<bool> ResetPasswordAsync(string code, string newPassword, Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
        {
            throw new ResourceNotFoundException("No such user", userId.ToString());
        }

        IdentityResult result = await _userManager.ResetPasswordAsync(user, code, newPassword);

        return result.Succeeded;
    }

    public async Task<Result<TokenModel>> RegisterFromInvitationAsync(RegisterInvitationModel registerInvitationModel)
    {
        // Get the invitation using IgnoreQueryFilters to bypass tenant filtering
        var invitation = await _dbContext.UserInvitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.InvitationToken == registerInvitationModel.InvitationCode);

        if (invitation == null)
        {
            return Result<TokenModel>.BusinessError("InvalidInvitation", "The invitation code is invalid or has expired.");
        }

        if (invitation.Used)
        {
            return Result<TokenModel>.BusinessError("InvitationAlreadyUsed", "This invitation has already been used.");
        }

        if (invitation.ExpiresOn < _timeProvider.GetUtcNow())
        {
            return Result<TokenModel>.BusinessError("InvitationExpired", "This invitation has expired.");
        }

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(invitation.Email);
        if (existingUser != null)
        {
            return Result<TokenModel>.BusinessError("UserExists", "A user with this email already exists.");
        }

        // Create the register model from the invitation
        var registerModel = new RegisterModel
        {
            Email = invitation.Email,
            FirstName = invitation.FirstName,
            LastName = invitation.LastName,
            Password = registerInvitationModel.Password,
            NormalizedTenantIdentifier = string.Empty, // Will be set from invitation tenant
        };

        // Create the user with the tenant from the invitation
        var userRegisterResult = await CreateUserAsync(registerModel, invitation.TenantId, true);

        if (userRegisterResult.IsFailure)
        {
            return Result<TokenModel>.Failure(userRegisterResult.ErrorModel);
        }

        // Assign roles from the invitation
        var roles = invitation.Roles.Select(Enum.Parse<AccountRoles>).ToArray();
        var roleAssignResult = await AssignRolesForNewUserAsync(userRegisterResult.GetValue, roles);

        if (roleAssignResult.IsFailure)
        {
            return Result<TokenModel>.BusinessError("RoleAssignment", "Could not assign roles to user.");
        }

        // Mark invitation as used
        invitation.Used = true;
        await _dbContext.SaveChangesAsync();

        // Generate JWT token for the new user
        var tokenModel = await GenerateJWTForUserAsync(userRegisterResult.GetValue.Id);

        return new Result<TokenModel>(tokenModel);
    }
}
