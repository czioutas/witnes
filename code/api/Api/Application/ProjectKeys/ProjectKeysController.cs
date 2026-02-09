using Api.Application.ProjectKeys.Models;
using Api.Application.ProjectKeys.Services;
using Api.Application.Tenancy.Services;
using Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.ProjectKeys;

/// <summary>
/// Controller for managing project keys
/// </summary>
[ApiController]
[Route("api/v1/project-keys")]
[Authorize]
public class ProjectKeysController : ControllerBase
{
    private readonly IProjectKeyService _projectKeyService;
    private readonly IRequestTenant _requestTenant;
    private readonly ApplicationDbContextRead _contextRead;
    private readonly TimeProvider _timeProvider;

    public ProjectKeysController(
        IProjectKeyService projectKeyService,
        IRequestTenant requestTenant,
        ApplicationDbContextRead contextRead,
        TimeProvider timeProvider)
    {
        _projectKeyService = projectKeyService ?? throw new ArgumentNullException(nameof(projectKeyService));
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _contextRead = contextRead ?? throw new ArgumentNullException(nameof(contextRead));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>
    /// Get all project keys for the current tenant
    /// </summary>
    /// <returns>List of project keys</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ProjectKeyModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProjectKeyModel>>> GetAll()
    {
        var result = await _projectKeyService.GetAllForTenantAsync(_requestTenant.TenantId);
        return !result.IsFailure ? Ok(result.GetValue) : NotFound();
    }

    /// <summary>
    /// Create a new project key for the current tenant
    /// </summary>
    /// <param name="request">Project key creation request</param>
    /// <returns>Created project key</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectKeyModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectKeyModel>> Create([FromBody] CreateProjectKeyRequest request)
    {
        var result = await _projectKeyService.CreateAsync(_requestTenant.TenantId, request);
        return !result.IsFailure ? Ok(result.GetValue) : BadRequest(result);
    }

    /// <summary>
    /// Update a project key
    /// </summary>
    /// <param name="id">Project key ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated project key</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProjectKeyModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectKeyModel>> Update(Guid id, [FromBody] UpdateProjectKeyRequest request)
    {
        var result = await _projectKeyService.UpdateAsync(id, request);
        return !result.IsFailure ? Ok(result.GetValue) : NotFound();
    }

    /// <summary>
    /// Deactivate a project key
    /// </summary>
    /// <param name="id">Project key ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Deactivate(Guid id)
    {
        var result = await _projectKeyService.DeactivateAsync(id);
        return !result.IsFailure ? NoContent() : NotFound();
    }

    /// <summary>
    /// Get usage statistics for the current tenant
    /// </summary>
    /// <returns>Usage statistics including total and current month page loads</returns>
    [HttpGet("usage")]
    [ProducesResponseType(typeof(UsageStatsModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsageStatsModel>> GetUsageStats()
    {
        var tenantId = _requestTenant.TenantId;
        var now = _timeProvider.GetUtcNow();
        var firstDayOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        // Get total page loads for the tenant (all time)
        var totalPageLoads = await _contextRead.MetricsGold
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .CountAsync();

        // Get current month page loads
        var currentMonthPageLoads = await _contextRead.MetricsGold
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.Timestamp >= firstDayOfMonth)
            .CountAsync();

        // Get last page load timestamp
        var lastPageLoad = await _contextRead.MetricsGold
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .OrderByDescending(m => m.Timestamp)
            .Select(m => m.Timestamp)
            .FirstOrDefaultAsync();

        var stats = new UsageStatsModel
        {
            TotalPageLoads = totalPageLoads,
            CurrentMonthPageLoads = currentMonthPageLoads,
            LastPageLoadAt = lastPageLoad != default ? lastPageLoad : null
        };

        return Ok(stats);
    }

    /// <summary>
    /// Get package and pricing information for the current tenant
    /// </summary>
    /// <returns>Package information including plan details, limits, and pricing</returns>
    [HttpGet("package")]
    [ProducesResponseType(typeof(PackageInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PackageInfoModel>> GetPackageInfo()
    {
        var tenantId = _requestTenant.TenantId;

        // Get the current active pricing tier for the tenant
        var tenantPricingTier = await _contextRead.TenantPricingTiers
            .AsNoTracking()
            .Include(tpt => tpt.PricingTier)
            .Where(tpt => tpt.TenantId == tenantId && tpt.IsActive)
            .OrderByDescending(tpt => tpt.StartDate)
            .FirstOrDefaultAsync();

        if (tenantPricingTier == null)
        {
            return NotFound(new { message = "No active pricing tier found for the current tenant" });
        }

        var pricingTier = tenantPricingTier.PricingTier;

        // Get current month page loads
        var now = _timeProvider.GetUtcNow();
        var firstDayOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var currentMonthPageLoads = await _contextRead.MetricsGold
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.Timestamp >= firstDayOfMonth)
            .CountAsync();

        // Calculate renewal date (start of next month)
        var renewalDate = firstDayOfMonth.AddMonths(1);

        var packageInfo = new PackageInfoModel
        {
            PlanName = pricingTier.Name,
            PlanDescription = pricingTier.Description,
            PageLoadsLimit = pricingTier.MonthlyRequestLimit,
            CurrentMonthPageLoads = currentMonthPageLoads,
            PricePerMonth = pricingTier.PricePerMonth,
            PricePerExtraRequest = pricingTier.PricePerExtraRequest,
            RenewalDate = renewalDate.UtcDateTime,
            IsActive = tenantPricingTier.IsActive
        };

        return Ok(packageInfo);
    }
}
