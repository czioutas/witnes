using Api.Application.Models;
using Api.Product.Visitors.Models;
using Libs.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Product.Visitors;

/// <summary>
/// Controller for retrieving visitor (end-user) analytics data.
/// These are the website visitors being monitored, NOT Witnes application users.
/// </summary>
[ApiController]
[Route("api/v1/visitors")]
[Authorize]
public class VisitorsController : ControllerBase
{
    private readonly IVisitorsService _visitorsService;
    private readonly ILogger<VisitorsController> _logger;

    public VisitorsController(
        IVisitorsService visitorsService,
        ILogger<VisitorsController> logger)
    {
        _visitorsService = visitorsService ?? throw new ArgumentNullException(nameof(visitorsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a paginated list of monitored visitors (end-users) with their activity summary.
    /// Supports filtering by user ID search, date range, and pagination.
    /// </summary>
    /// <param name="request">Filter and pagination parameters</param>
    /// <returns>Paginated result containing visitor summaries</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/v1/visitors?userIdSearch=user123&amp;startDate=2026-01-01&amp;pageNumber=1&amp;pageSize=20
    ///
    /// Returns visitors ordered by most recently seen (LastSeenAt descending).
    ///
    /// **Note:** Browsers and OperatingSystems fields are empty in the current MVP.
    /// These will be populated in future enhancements when device info is added to the tracking system.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<VisitorSummaryModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<VisitorSummaryModel>>> GetVisitors([FromQuery] GetVisitorsRequest request)
    {
        _logger.LogInformation(
            "Getting visitors with filters: UserIdSearch={UserIdSearch}, StartDate={StartDate}, EndDate={EndDate}, PageNumber={PageNumber}, PageSize={PageSize}",
            request.UserIdSearch,
            request.StartDate,
            request.EndDate,
            request.PageNumber,
            request.PageSize);

        var result = await _visitorsService.GetVisitorsAsync(request);

        _logger.LogInformation(
            "Retrieved {Count} visitors out of {TotalCount} (Page {PageNumber}/{TotalPages})",
            result.Data.Count,
            result.TotalCount,
            result.PageNumber,
            result.TotalPages);

        return Ok(result);
    }

    /// <summary>
    /// Gets a paginated list of page loads for a specific visitor (end-user).
    /// Supports filtering by date range and pagination.
    /// </summary>
    /// <param name="userId">The user ID to retrieve page loads for</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <returns>Paginated result containing page load summaries</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/v1/visitors/user123/page-loads?pageNumber=1&amp;pageSize=20&amp;startDate=2026-01-01
    ///
    /// Returns page loads ordered by most recent first (Timestamp descending).
    /// </remarks>
    [HttpGet("{userId}/page-loads")]
    [ProducesResponseType(typeof(PagedResult<PageLoadSummaryModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<PageLoadSummaryModel>>> GetUserPageLoads(
        string userId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        _logger.LogInformation(
            "Getting page loads for user: UserId={UserId}, StartDate={StartDate}, EndDate={EndDate}, PageNumber={PageNumber}, PageSize={PageSize}",
            userId,
            startDate,
            endDate,
            pageNumber,
            pageSize);

        var request = new GetUserPageLoadsRequest
        {
            UserOrGuestId = userId,
            PageNumber = pageNumber,
            PageSize = pageSize,
            StartDate = startDate,
            EndDate = endDate
        };

        var result = await _visitorsService.GetUserPageLoadsAsync(request);

        _logger.LogInformation(
            "Retrieved {Count} page loads out of {TotalCount} for user {UserId} (Page {PageNumber}/{TotalPages})",
            result.Data.Count,
            result.TotalCount,
            userId,
            result.PageNumber,
            result.TotalPages);

        return Ok(result);
    }
}
