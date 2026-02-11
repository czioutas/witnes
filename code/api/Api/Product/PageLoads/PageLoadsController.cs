using Api.Application.Models;
using Api.Product.PageLoads.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Product.PageLoads;

/// <summary>
/// Controller for retrieving detailed page load data including network waterfall and jank metrics
/// </summary>
[ApiController]
[Route("v1/page-loads")]
[Authorize]
public class PageLoadsController : ControllerBase
{
    private readonly IPageLoadsService _pageLoadsService;
    private readonly ILogger<PageLoadsController> _logger;

    public PageLoadsController(
        IPageLoadsService pageLoadsService,
        ILogger<PageLoadsController> logger)
    {
        _pageLoadsService = pageLoadsService ?? throw new ArgumentNullException(nameof(pageLoadsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets detailed information for a specific page load including network waterfall and jank reports
    /// </summary>
    /// <param name="id">The page load ID (silver layer ID)</param>
    /// <returns>Detailed page load model</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/v1/page-loads/019c3eb8-7a1f-7a29-af77-963722c894b6
    ///
    /// Returns detailed metrics including:
    /// - Core Web Vitals (LCP, CLS, TTFB)
    /// - Network waterfall (all resources loaded with timings)
    /// - Jank reports (performance issues detected)
    /// </remarks>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PageLoadDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PageLoadDetailModel>> GetPageLoadDetail(Guid id)
    {
        _logger.LogInformation("Getting page load detail for ID: {PageLoadId}", id);

        var detail = await _pageLoadsService.GetPageLoadDetailAsync(id);

        if (detail == null)
        {
            _logger.LogWarning("Page load not found: {PageLoadId}", id);
            return NotFound();
        }

        _logger.LogInformation("Retrieved page load detail for: {PageLoadId}", id);
        return Ok(detail);
    }
}
