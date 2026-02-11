using Api.Application.Authentication;
using Api.Application.Pricing.Models;
using Api.Application.Pricing.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Application.Pricing;

/// <summary>
/// Controller for managing tenant pricing tiers. Only accessible by admin users.
/// </summary>
[ApiController]
[Route("/v1/[controller]")]
[Authorize(Roles = nameof(AccountRoles.AdminUserRole))]
#if DEBUG
[ApiExplorerSettings(GroupName = "v1", IgnoreApi = false)]
#else
[ApiExplorerSettings(GroupName = "v1", IgnoreApi = true)]
#endif
public class PricingTenantController : ControllerBase
{
    private readonly ITenantPricingService _tenantPricingService;
    private readonly ILogger<PricingTenantController> _logger;

    public PricingTenantController(
        ITenantPricingService tenantPricingService,
        ILogger<PricingTenantController> logger)
    {
        _tenantPricingService = tenantPricingService;
        _logger = logger;
    }

    /// <summary>
    /// Sets the initial pricing tier for a tenant.
    /// </summary>
    /// <param name="request">The pricing tier configuration</param>
    /// <returns>The created tenant pricing tier entity</returns>
    /// <response code="200">Pricing tier set successfully</response>
    /// <response code="400">Invalid request or pricing tier already exists for the specified date</response>
    /// <response code="401">Unauthorized - admin access required</response>
    /// <response code="404">Pricing tier not found</response>
    [HttpPost("pricing")]
    public async Task<IActionResult> SetTenantPricing([FromBody] SetTenantPricingRequest request)
    {
        var result = await _tenantPricingService.SetTenantPricingAsync(
            request.PricingTierId,
            request.StartDate);

        if (result.IsFailure)
        {
            return BadRequest(result.ErrorModel);
        }

        return Ok(result.GetValue);
    }

    /// <summary>
    /// Changes the pricing tier for a tenant. Automatically ends the current pricing tier.
    /// </summary>
    /// <param name="request">The new pricing tier configuration</param>
    /// <returns>The new tenant pricing tier entity</returns>
    /// <response code="200">Pricing tier changed successfully</response>
    /// <response code="400">Invalid request or new pricing tier not found</response>
    /// <response code="401">Unauthorized - admin access required</response>
    /// <response code="404">New pricing tier not found</response>
    [HttpPut("pricing")]
    public async Task<IActionResult> ChangeTenantPricing([FromBody] ChangeTenantPricingRequest request)
    {
        var result = await _tenantPricingService.ChangeTenantPricingAsync(
            request.NewPricingTierId,
            request.StartDate);

        if (result.IsFailure)
        {
            return BadRequest(result.ErrorModel);
        }

        return Ok(result.GetValue);
    }

    /// <summary>
    /// Gets the current active pricing tier for a tenant.
    /// </summary>
    /// <returns>The current tenant pricing tier entity</returns>
    /// <response code="200">Current pricing tier retrieved successfully</response>
    /// <response code="401">Unauthorized - admin access required</response>
    /// <response code="404">No active pricing found for tenant</response>
    [HttpGet("pricing/current")]
    public async Task<IActionResult> GetCurrentTenantPricing()
    {
        var pricing = await _tenantPricingService.GetCurrentTenantPricingAsync();

        if (pricing == null)
        {
            return NotFound("No active pricing found for tenant");
        }

        return Ok(pricing);
    }

    /// <summary>
    /// Gets the complete pricing history for a tenant.
    /// </summary>
    /// <returns>List of all pricing tiers for the tenant, ordered by start date</returns>
    /// <response code="200">Pricing history retrieved successfully</response>
    /// <response code="401">Unauthorized - admin access required</response>
    [HttpGet("pricing/history")]
    public async Task<IActionResult> GetTenantPricingHistory()
    {
        var history = await _tenantPricingService.GetTenantPricingHistoryAsync();
        return Ok(history);
    }

    /// <summary>
    /// Gets the pricing tier that was active for a tenant on a specific date.
    /// </summary>
    /// <param name="date">The date to check pricing for</param>
    /// <returns>The tenant pricing tier entity that was active on the specified date</returns>
    /// <response code="200">Pricing tier retrieved successfully</response>
    /// <response code="401">Unauthorized - admin access required</response>
    /// <response code="404">No pricing found for tenant on the specified date</response>
    [HttpGet("pricing/date/{date}")]
    public async Task<IActionResult> GetTenantPricingAtDate(DateOnly date)
    {
        var pricing = await _tenantPricingService.GetTenantPricingAtDateAsync(date);

        if (pricing == null)
        {
            return NotFound($"No pricing found for tenant on {date}");
        }

        return Ok(pricing);
    }
}
