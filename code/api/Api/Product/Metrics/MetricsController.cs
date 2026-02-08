using Api.Application.Filters;
using Api.Product.Metrics.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Product.Metrics;

/// <summary>
/// Controller for retrieving speed metrics data
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class MetricsController : ControllerBase
{
    private readonly IMetricsService _metricsService;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(
        IMetricsService metricsService,
        ILogger<MetricsController> logger)
    {
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all speed metrics for the current tenant
    /// </summary>
    /// <returns>List of speed metrics</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<SpeedMetricResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SpeedMetricResponse>>> GetAll()
    {
        _logger.LogInformation("Getting all speed metrics");

        var metrics = await _metricsService.GetAllMetricsAsync();

        return Ok(metrics);
    }

    /// <summary>
    /// Gets a specific speed metric by ID
    /// </summary>
    /// <param name="id">Metric ID</param>
    /// <returns>Speed metric details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SpeedMetricResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpeedMetricResponse>> GetById(Guid id)
    {
        _logger.LogInformation("Getting speed metric by Id={Id}", id);

        var metric = await _metricsService.GetMetricByIdAsync(id);

        if (metric == null)
        {
            return NotFound();
        }

        return Ok(metric);
    }
}
