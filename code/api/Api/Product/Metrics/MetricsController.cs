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
