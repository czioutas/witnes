using Api.Application.Filters;
using Api.Product.Metrics.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Product.Metrics;

/// <summary>
/// Controller for retrieving metrics data
/// </summary>
[ApiController]
[Route("v1/[controller]")]
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
    /// Gets all metrics for the current tenant
    /// </summary>
    /// <returns>List of metrics</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<MetricResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MetricResponse>>> GetAll()
    {
        _logger.LogInformation("Getting all metrics");

        var metrics = await _metricsService.GetAllMetricsAsync();

        return Ok(metrics);
    }

    /// <summary>
    /// Gets a specific metric by ID
    /// </summary>
    /// <param name="id">Metric ID</param>
    /// <returns>Metric details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MetricResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MetricResponse>> GetById(Guid id)
    {
        _logger.LogInformation("Getting metric by Id={Id}", id);

        var metric = await _metricsService.GetMetricByIdAsync(id);

        if (metric == null)
        {
            return NotFound();
        }

        return Ok(metric);
    }

    [HttpPost("recalculate-stage/{stage}/tenant/{tenantId}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ApiExplorerSettings(GroupName = "v1", IgnoreApi = true)]
    public async Task<IActionResult> RecalculateStage(string stage, Guid tenantId)
    {
        _logger.LogInformation("Recalculating Stage={Stage} for TenantId={TenantId}", stage, tenantId);

        var success = await _metricsService.RecalculateStageForTenantAsync(tenantId, stage);

        if (!success)
        {
            return BadRequest($"Invalid stage: {stage}. Valid stages are 'silver' and 'gold'.");
        }

        return Accepted();
    }
}
