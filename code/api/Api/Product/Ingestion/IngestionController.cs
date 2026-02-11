using Api.Application.Tenancy.Services;
using Api.Product.DailySalt;
using Api.Product.Ingestion.Events;
using Api.Product.Ingestion.Models;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Api.Product.Ingestion;

/// <summary>
/// Controller for ingesting metrics
/// </summary>
[ApiController]
[Route("v1/events")]
// [Authorize]
public class IngestionController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<IngestionController> _logger;
    private readonly IRequestTenant _requestTenant;
    private readonly IVisitorHashService _visitorHashService;
    private readonly TimeProvider _timeProvider;

    public IngestionController(
        IPublishEndpoint publishEndpoint,
        ILogger<IngestionController> logger,
        IRequestTenant requestTenant,
        IVisitorHashService visitorHashService,
        TimeProvider timeProvider)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _visitorHashService = visitorHashService ?? throw new ArgumentNullException(nameof(visitorHashService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>
    /// Ingests a metric
    /// </summary>
    /// <param name="request">Metric data</param>
    /// <returns>Acknowledgment response</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> IngestMetric([FromBody] IngestMetricRequestModel request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        var hashId = await _visitorHashService.ComputeHashIdAsync(ip, userAgent);

        // Publish event to message queue for Bronze processing
        await _publishEndpoint.Publish(new MetricIngestedEvent
        {
            Event = request,
            TenantId = _requestTenant.TenantId, // Set by ProjectKeyMiddleware
            IngestedAt = _timeProvider.GetUtcNow(),
            HashId = hashId
        });

        _logger.LogInformation("Metric ingestion acknowledged");

        return Accepted();
    }
}
