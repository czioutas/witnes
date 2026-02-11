using Api.Application.Account.Models;
using Api.Application.Account.Services;
using Api.Application.Account.Services.Interfaces;
using Api.Application.Extensions;
using Api.Application.Models;
using Libs.Exceptions;
using Libs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Application.Account;

/// <summary>
/// User account management and authentication endpoints
/// </summary>
/// <remarks>
/// Provides comprehensive user account lifecycle management including registration, authentication,
/// profile management, and password recovery. These endpoints handle the core user authentication
/// flow and account operations for the Witnes platform.
/// 
/// **Security Features:**
/// - JWT-based authentication with refresh tokens
/// - Email verification for new accounts
/// - Secure password reset workflow
/// - Role-based authorization
/// 
/// **Account Lifecycle:**
/// 1. User registration with email verification
/// 2. Login with email/password credentials
/// 3. Profile updates and token refresh
/// 4. Password reset when needed
/// </remarks>
[ApiController]
[Route("/v1/[controller]/[action]")]
[ApiExplorerSettings(GroupName = "v1")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<AccountController> _logger;
    private readonly IAccountService _accountService;

    public AccountController(
        IAccountService accountService,
        ITokenService tokenService,
        ILogger<AccountController> logger)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register a new user account on the Witnes platform
    /// </summary>
    /// <remarks>
    /// Creates a new user account with the provided registration details. Upon successful registration:
    /// - A verification email is sent to the user's email address (unless using invitation token)
    /// - JWT access and refresh tokens are immediately generated for the new user
    /// - The user can begin using the platform while email verification is pending
    /// 
    /// **Registration Process:**
    /// 1. Validate registration data (email uniqueness, password strength, etc.)
    /// 2. Create user account with pending email verification status
    /// 3. Send verification email (if not using invitation token)
    /// 4. Generate JWT tokens for immediate platform access
    /// 
    /// **Invitation Flow:**
    /// If an invitation token is provided, email verification is skipped and the account is immediately activated.
    /// </remarks>
    /// <param name="registerModel">Registration details including email, password, name, and optional invitation token</param>
    /// <returns>JWT access and refresh tokens for the newly created user account</returns>
    /// <response code="200">Registration successful - returns authentication tokens</response>
    /// <response code="400">Invalid registration data (email already exists, weak password, etc.)</response>
    /// <response code="500">Internal server error during account creation</response>
    [HttpPost]
    [ProducesResponseType(typeof(TokenModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
#if DEBUG
    [ApiExplorerSettings(GroupName = "v1", IgnoreApi = false)]
#else
    [ApiExplorerSettings(GroupName = "v1", IgnoreApi = true)]
#endif
    public async Task<ActionResult<TokenModel>> Register(RegisterModel registerModel)
    {
        var newRegisteredUserResult = await _accountService.RegisterAsync(registerModel, null);

        if (newRegisteredUserResult.IsFailure)
        {
            var problem = newRegisteredUserResult.ErrorModel.ToApplicationProblemDetails(HttpContext);
            return StatusCode(problem.Status ?? 400, problem);
        }

        if (string.IsNullOrEmpty(registerModel.InvitationToken))
        {
            _ = _accountService.SendVerificationEmailAsync(newRegisteredUserResult.GetValue.FirstName, newRegisteredUserResult.GetValue.Id);
        }

        var tokenModel = await _accountService.GenerateJWTForUserAsync(newRegisteredUserResult.GetValue.Id);
        return Ok(tokenModel);
    }

    /// <summary>
    /// Authenticate user credentials and obtain access tokens
    /// </summary>
    /// <remarks>
    /// Validates user email and password credentials, then returns JWT tokens for authenticated access.
    /// This endpoint is the primary authentication mechanism for existing users.
    /// 
    /// **Authentication Process:**
    /// 1. Validate email and password against stored credentials
    /// 2. Check account status (active, email verified, not locked)
    /// 3. Generate new JWT access and refresh tokens
    /// 4. Return tokens for subsequent API requests
    /// 
    /// **Token Usage:**
    /// - Access Token: Include in Authorization header for API requests
    /// - Refresh Token: Use to obtain new access tokens when they expire
    /// 
    /// **Security Notes:**
    /// - Failed login attempts may trigger account lockout mechanisms
    /// - Tokens have configurable expiration times for security
    /// </remarks>
    /// <param name="loginModel">User login credentials (email and password)</param>
    /// <returns>JWT access and refresh tokens for authenticated user</returns>
    /// <response code="200">Login successful - returns authentication tokens</response>
    /// <response code="400">Invalid request format or missing required fields</response>
    /// <response code="401">Invalid credentials or account locked/disabled</response>
    /// <response code="415">Unsupported media type - ensure Content-Type is application/json</response>
    /// <response code="500">Internal server error during authentication</response>
    [HttpPost]
    [ProducesResponseType(typeof(TokenModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<ActionResult<TokenModel>> Login(LoginModel loginModel)
    {
        var result = await _accountService.LoginAsync(loginModel.Email, loginModel.Password);

        if (result.IsFailure)
        {
            return Unauthorized(new ApplicationProblemDetailsModel(
                statusCode: 401,
                title: "Unathorized",
                exceptionType: "AuthenticationException",
                path: HttpContext.Request.Path,
                traceId: HttpContext.TraceIdentifier,
                errors: [result.ErrorModel?.Message ?? "Invalid credentials"]
            ));
        }

        return Ok(result.GetValue);
    }

    /// <summary>
    /// Update user account profile information
    /// </summary>
    /// <remarks>
    /// Updates the authenticated user's account profile with new information. After successful update,
    /// new JWT tokens are generated to reflect any changes in user claims or data.
    /// 
    /// **Updatable Fields:**
    /// - Name (first name, last name)
    /// - Profile preferences
    /// - Contact information
    /// - Account settings
    /// 
    /// **Authentication Required:**
    /// This endpoint requires a valid JWT token in the Authorization header.
    /// The user can only update their own profile information.
    /// 
    /// **Token Refresh:**
    /// New tokens are returned after successful update to ensure JWT claims reflect the latest user data.
    /// </remarks>
    /// <param name="userModel">Updated account information to be saved</param>
    /// <returns>New JWT tokens reflecting the updated account information</returns>
    /// <response code="200">Profile updated successfully - returns new authentication tokens</response>
    /// <response code="400">Invalid update data or validation errors</response>
    /// <response code="401">Unauthorized - valid authentication token required</response>
    /// <response code="500">Internal server error during profile update</response>
    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
#if DEBUG
    [ApiExplorerSettings(GroupName = "v1", IgnoreApi = false)]
#else
    [ApiExplorerSettings(GroupName = "v1", IgnoreApi = true)]
#endif
    public async Task<ActionResult<TokenModel>> Details(UpdateAccountModel userModel)
    {
        await _accountService.PatchAsync(HttpContext.GetUserId(), userModel);
        TokenModel tokenModel = await _accountService.GetTokenForUser(HttpContext.GetUserId());
        return Ok(tokenModel);
    }

    /// <summary>
    /// Refresh expired access token using refresh token
    /// </summary>
    /// <remarks>
    /// Exchanges a valid refresh token for new access and refresh tokens. This allows users to maintain
    /// authenticated sessions without re-entering credentials when access tokens expire.
    /// 
    /// **Token Refresh Process:**
    /// 1. Validate the provided refresh token
    /// 2. Verify token hasn't been revoked or expired
    /// 3. Generate new access and refresh token pair
    /// 4. Invalidate the old refresh token (single-use)
    /// 
    /// **Security Considerations:**
    /// - Refresh tokens are single-use and automatically invalidated
    /// - Both tokens have independent expiration times
    /// - Invalid/expired refresh tokens return 401 Unauthorized
    /// </remarks>
    /// <param name="refreshToken">Valid refresh token to exchange for new tokens</param>
    /// <returns>New access and refresh token pair</returns>
    /// <response code="200">Token refresh successful - returns new authentication tokens</response>
    /// <response code="400">Invalid or malformed refresh token</response>
    /// <response code="401">Refresh token expired, revoked, or invalid</response>
    /// <response code="500">Internal server error during token refresh</response>
    [AllowAnonymous]
    [HttpGet("{refreshToken}")]
    [ProducesResponseType(typeof(TokenModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
    [ApiExplorerSettings(GroupName = "v1", IgnoreApi = false)]
    public ActionResult<TokenModel> RefreshToken(string refreshToken)
    {
        TokenModel tokenModel = _tokenService.RefreshToken(refreshToken);
        return Ok(tokenModel);
    }

    [HttpPost("/v1/verify-email/")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status404NotFound)]
    [AllowAnonymous]
#if DEBUG
    [ApiExplorerSettings(GroupName = "v1", IgnoreApi = false)]
#else
    [ApiExplorerSettings(GroupName = "v1", IgnoreApi = true)]
#endif
    public async Task<ActionResult> VerifyEmail(VerifyEmailModel model)
    {
        return await _accountService.VerifyEmailAsync(model.Code, model.UserId) ? Ok() : NotFound();
    }

    [HttpGet("/v1/request-reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status404NotFound)]
    [AllowAnonymous]
#if DEBUG
    [ApiExplorerSettings(GroupName = "v1", IgnoreApi = false)]
#else
    [ApiExplorerSettings(GroupName = "v1", IgnoreApi = true)]
#endif
    public async Task<ActionResult> RequestResetPassword([FromQuery] string email)
    {
        await _accountService.RequestPasswordResetAsync(email);
        return Ok();
    }

    [HttpPost("/v1/reset-password/")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status404NotFound)]
    [AllowAnonymous]
#if DEBUG
    [ApiExplorerSettings(GroupName = "v1", IgnoreApi = false)]
#else
    [ApiExplorerSettings(GroupName = "v1", IgnoreApi = true)]
#endif
    public async Task<ActionResult> ResetPassword(ResetPasswordModel model)
    {
        return await _accountService.ResetPasswordAsync(model.Code, model.NewPassword, model.UserId) ? Ok() : NotFound();
    }

    /// <summary>
    /// Complete user registration using an invitation code
    /// </summary>
    /// <remarks>
    /// Completes the registration process for users who received an invitation.
    /// This endpoint retrieves user details from the invitation and creates the account
    /// without requiring tenant context since the invitation is looked up globally.
    /// </remarks>
    /// <param name="registerInvitationModel">Invitation code and password</param>
    /// <returns>JWT access and refresh tokens for the newly created user account</returns>
    /// <response code="200">Registration successful - returns authentication tokens</response>
    /// <response code="400">Invalid invitation code or registration data</response>
    /// <response code="500">Internal server error during account creation</response>
    [HttpPost]
    [ProducesResponseType(typeof(TokenModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
#if DEBUG
    [ApiExplorerSettings(GroupName = "v1", IgnoreApi = false)]
#else
    [ApiExplorerSettings(GroupName = "v1", IgnoreApi = true)]
#endif
    public async Task<ActionResult<TokenModel>> RegisterInvitation(RegisterInvitationModel registerInvitationModel)
    {
        var result = await _accountService.RegisterFromInvitationAsync(registerInvitationModel);

        if (result.IsFailure)
        {
            return BadRequest(new ApplicationProblemDetailsModel(
                statusCode: 400,
                title: "Registration Failed",
                exceptionType: "RegistrationException",
                path: HttpContext.Request.Path,
                traceId: HttpContext.TraceIdentifier,
                errors: [result.ErrorModel?.Message ?? "Could not complete registration"]
            ));
        }

        return Ok(result.GetValue);
    }

    /// <summary>
    /// Get current user's profile information
    /// </summary>
    /// <remarks>
    /// Retrieves the authenticated user's profile information including personal details,
    /// account status, and preferences. This endpoint provides the current state of the
    /// user's account as stored in the system.
    /// 
    /// **Returned Information:**
    /// - User identification (ID, email)
    /// - Personal details (name, preferences)
    /// - Account status (verified, active)
    /// - Role and permissions
    /// - Last login information
    /// 
    /// **Authentication Required:**
    /// This endpoint requires a valid JWT token. Users can only access their own profile data.
    /// </remarks>
    /// <returns>Current user's profile information</returns>
    /// <response code="200">Successfully retrieved user profile information</response>
    /// <response code="401">Unauthorized - valid authentication token required</response>
    /// <response code="404">User profile not found (account may have been deleted)</response>
    /// <response code="500">Internal server error retrieving profile</response>
    [HttpGet()]
    [ProducesResponseType(typeof(SlimApplicationUserModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
    [ApiExplorerSettings(GroupName = "v1", IgnoreApi = false)]
    public async Task<ActionResult<SlimApplicationUserModel>> Self()
    {
        var result = await _accountService.GetSelfAsync(HttpContext.GetUserId());

        if (result.IsFailure)
        {
            return NotFound();
        }

        return Ok(result.GetValue);
    }
}
