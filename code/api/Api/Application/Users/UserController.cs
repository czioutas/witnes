using Api.Application.Account.Models;
using Api.Application.Authentication;
using Api.Application.Extensions;
using Api.Application.Models;
using Api.Application.Tenancy.Services;
using Api.Application.Users.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Usersr.API.Users.Services;

namespace Usersr.API.Users.Controllers;

[Route("/v1/[controller]")]
#if DEBUG
[ApiExplorerSettings(GroupName = "v1", IgnoreApi = false)]
#else
[ApiExplorerSettings(GroupName = "v1", IgnoreApi = true)]
#endif  
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUsersService _service;
    private readonly IRequestTenant _requestTenant;

    public UserController(IRequestTenant requestTenant, IUsersService service)
    {
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpPut("{id}", Name = "UserUpdate")]
    [Authorize(Roles = nameof(AccountRoles.AdminUserRole))]
    [ProducesResponseType(typeof(SlimApplicationUserModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SlimApplicationUserModel>> Update(Guid id, [FromBody] UpdateUserModel model)
    {
        if (id != model.UserId)
        {
            return BadRequest("ID mismatch");
        }

        var result = await _service.UpdateUserAsync(model);

        if (result.IsFailure)
        {
            return BadRequest();
        }

        return Ok(result.GetValue);
    }

    [HttpGet(Name = "UserGetAll")]
    [Authorize(Roles = nameof(AccountRoles.AdminUserRole))]
    [ProducesResponseType(typeof(List<SlimApplicationUserModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<SlimApplicationUserModel>>> GetAll()
    {
        var users = await _service.GetAllAsync(HttpContext.GetTenantId());
        return Ok(users);
    }

    [HttpGet("{id}", Name = "UserGet")]
    [Authorize(Roles = nameof(AccountRoles.AdminUserRole))]
    [ProducesResponseType(typeof(SlimApplicationUserModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SlimApplicationUserModel>> Get(Guid id)
    {
        if (!HttpContext.IsAdmin() && HttpContext.GetUserId() != id)
        {
            throw new UnauthorizedAccessException();
        }

        var result = await _service.GetAsync(id, HttpContext.GetTenantId());
        if (result.IsFailure)
        {
            return NotFound();
        }
        return Ok(result.GetValue);
    }


    [HttpGet("invitations", Name = "UserInvitationsPendingGetAll")]
    [Authorize(Roles = nameof(AccountRoles.AdminUserRole))]
    [ProducesResponseType(typeof(List<UserInvitationModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<UserInvitationModel>>> UserInvitationsGetAll()
    {
        var userInvitations = await _service.GetPendingUserInvitationsAsync();
        return Ok(userInvitations);
    }

    [HttpPost(Name = "UserInvite")]
    [Authorize(Roles = nameof(AccountRoles.AdminUserRole))]
    [ProducesResponseType(typeof(UserInvitationModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserInvitationModel>> Invite(CreateUserInvitationModel createUserInvitationModel)
    {
        _requestTenant.SetTenantId(HttpContext.GetTenantId());
        var result = await _service.InviteUserAsync(createUserInvitationModel, HttpContext.GetUserId());

        if (result.IsFailure)
        {
            return UnprocessableEntity();
        }

        return Ok(result.GetValue);
    }

    [HttpDelete("{id}", Name = "UserDelete")]
    [Authorize(Roles = nameof(AccountRoles.AdminUserRole))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteUserAsync(id, HttpContext.GetTenantId(), HttpContext.GetUserId());

        if (result.IsFailure)
        {
            return BadRequest(result.ErrorModel);
        }

        return NoContent();
    }

    [HttpDelete("invitations/{id}", Name = "UserInvitationDelete")]
    [Authorize(Roles = nameof(AccountRoles.AdminUserRole))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteInvitation(Guid id)
    {
        var result = await _service.DeleteUserInvitationAsync(id, HttpContext.GetTenantId());

        if (result.IsFailure)
        {
            return BadRequest(result.ErrorModel);
        }

        return NoContent();
    }
}
