using Api.Application.Tenancy.Services;
using Api.Product.Ingestion.Events;
using Api.Product.Ingestion.Models;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Api.Product.Ingestion;

/// <summary>
/// Controller for ingesting speed metrics
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
// [Authorize]
public class IngestionController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<IngestionController> _logger;
    private readonly IRequestTenant _requestTenant;

    public IngestionController(
        IPublishEndpoint publishEndpoint,
        ILogger<IngestionController> logger,
        IRequestTenant requestTenant)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
    }

    /// <summary>
    /// Ingests a speed metric
    /// </summary>
    /// <param name="request">Speed metric data</param>
    /// <returns>Acknowledgment response</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> IngestSpeedMetric([FromBody] IngestSpeedMetricRequestModel request)
    {
        // Publish event to message queue for Bronze processing
        await _publishEndpoint.Publish(new SpeedMetricIngestedEvent
        {
            Event = request,
            TenantId = _requestTenant.TenantId, // Set by ProjectKeyMiddleware
            IngestedAt = DateTime.UtcNow
        });

        _logger.LogInformation("Speed metric ingestion acknowledged");

        return Accepted();
    }
}
