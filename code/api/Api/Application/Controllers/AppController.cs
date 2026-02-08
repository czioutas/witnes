using Api.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Application.Controllers;

/// <summary>
/// Application health and status endpoints
/// </summary>
/// <remarks>
/// Provides basic application health checks and status information for monitoring and diagnostics.
/// These endpoints are essential for load balancers, monitoring systems, and operational health checks.
/// </remarks>
[ApiController]
[Route("/v1")]
#if DEBUG
[ApiExplorerSettings(GroupName = "v1", IgnoreApi = false)]
#else
[ApiExplorerSettings(GroupName = "v1", IgnoreApi = true)]
#endif 
public class AppController : ControllerBase
{
    private readonly ILogger<AppController> _logger;

    public AppController(ILogger<AppController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Health check endpoint to verify API availability
    /// </summary>
    /// <remarks>
    /// Simple ping endpoint that returns a success response to indicate the API is running and accessible.
    /// This endpoint is commonly used by:
    /// - Load balancers for health checks
    /// - Monitoring systems for uptime verification
    /// - CI/CD pipelines for deployment validation
    /// - Development teams for basic connectivity testing
    /// 
    /// The response is lightweight and does not perform any complex operations or database checks.
    /// </remarks>
    /// <returns>A simple integer response (1) indicating the service is healthy</returns>
    /// <response code="200">Service is healthy and responding normally</response>
    /// <response code="500">Internal server error - service may be experiencing issues</response>
    /// <example>
    /// GET /v1/ping
    /// 
    /// Response: 1
    /// </example>
    [HttpGet("ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
    public ActionResult<int> Ping()
    {
        _logger.LogInformation("Ping");

        return 1;
    }
}
