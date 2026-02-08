using Api.Application.Features.Models;
using Api.Application.Models;
using Libs.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace Api.Application.Features;

[ApiController]
[Route("v1/features")]
[Authorize]
[ApiExplorerSettings(GroupName = "v1", IgnoreApi = false)]
public class FeaturesController : ControllerBase
{
    private readonly IFeatureManager _featureManager;
    private readonly ILogger<FeaturesController> _logger;

    public FeaturesController(
        IFeatureManager featureManager,
        ILogger<FeaturesController> logger)
    {
        _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    [ProducesResponseType(typeof(EnabledFeaturesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EnabledFeaturesResponse>> GetEnabledFeatures(CancellationToken cancellationToken)
    {
        try
        {
            var enabledFeatures = new List<FeatureKey>();

            await foreach (var featureName in _featureManager.GetFeatureNamesAsync())
            {
                if (await _featureManager.IsEnabledAsync(featureName))
                {
                    if (Enum.TryParse<FeatureKey>(featureName, true, out var featureKey))
                    {
                        enabledFeatures.Add(featureKey);
                    }
                }
            }

            return Ok(new EnabledFeaturesResponse { Features = enabledFeatures });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enabled features");

            var problem = new ApplicationProblemDetailsModel(
                    statusCode: 500,
                    title: "Internal Server Error",
                    exceptionType: ex.GetType().Name,
                    path: HttpContext.Request.Path,
                    traceId: HttpContext.TraceIdentifier,
                    errors: Array.Empty<string>()
                );

            return StatusCode(500, problem);
        }
    }

    [HttpGet("{featureKey}")]
    [ProducesResponseType(typeof(CheckFeatureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApplicationProblemDetailsModel), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CheckFeatureResponse>> CheckFeature(FeatureKey featureKey, CancellationToken cancellationToken)
    {
        try
        {
            var isEnabled = await _featureManager.IsEnabledAsync(featureKey.ToString());
            return Ok(new CheckFeatureResponse { FeatureKey = featureKey, IsEnabled = isEnabled });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature {FeatureKey}", featureKey);
            var problem = new ApplicationProblemDetailsModel(
                    statusCode: 500,
                    title: "Internal Server Error",
                    exceptionType: ex.GetType().Name,
                    path: HttpContext.Request.Path,
                    traceId: HttpContext.TraceIdentifier,
                    errors: Array.Empty<string>()
                );

            return StatusCode(500, problem);
        }
    }
}
