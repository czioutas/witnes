using Api.Application.Authentication;
using Api.Application.Extensions;
using Api.Application.Models;
using Api.Application.Services;
using Api.Application.Tenancy.Models;
using Api.Application.Tenancy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Application.Tenancy;

[ApiController]
[Authorize(Roles = nameof(AccountRoles.AdminUserRole))]
[Route("/v1/[controller]")]
#if DEBUG
[ApiExplorerSettings(GroupName = "v1", IgnoreApi = false)]
#else
[ApiExplorerSettings(GroupName = "v1", IgnoreApi = true)]
#endif
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantController> _logger;

    public TenantController(
        ITenantService tenantService,
        ILogger<TenantController> logger)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    [ProducesResponseType(typeof(TenantModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TenantModel>> Get()
    {
        var result = await _tenantService.GetAsync(tenantId: HttpContext.GetTenantId(), userId: HttpContext.GetUserId());

        if (result.IsFailure)
        {
            return NotFound();
        }

        return Ok(result.GetValue);
    }

    [HttpPut]
    [ProducesResponseType(typeof(TenantModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TenantModel>> Update([FromBody] UpdateTenantDetailsRequest request)
    {
        var result = await _tenantService.UpdateTenantDetailsAsync(HttpContext.GetTenantId(), request);

        if (result.IsFailure)
        {
            return NotFound();
        }

        return Ok(result.GetValue);
    }
}
