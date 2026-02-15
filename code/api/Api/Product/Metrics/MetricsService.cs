using Api.Product.MetricsProcessing.Gold;
using Api.Product.MetricsProcessing.Silver;
using MassTransit;

namespace Api.Product.Metrics;

/// <summary>
/// Service interface for retrieving metrics data
/// </summary>
public interface IMetricsService
{
    Task<bool> RecalculateStageForTenantAsync(Guid tenantId, string stage);
}

/// <summary>
/// Service for retrieving metrics data from Gold layer
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MetricsService> _logger;

    public MetricsService(
        IPublishEndpoint publishEndpoint,
        ILogger<MetricsService> logger)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> RecalculateStageForTenantAsync(Guid tenantId, string stage)
    {
        switch (stage.ToLower())
        {
            case "silver":
                _logger.LogInformation("Publishing RecalculateSilverEvent for TenantId={TenantId}", tenantId);
                await _publishEndpoint.Publish(new RecalculateSilverEvent { TenantId = tenantId });
                return true;
            case "gold":
                _logger.LogInformation("Publishing RecalculateGoldEvent for TenantId={TenantId}", tenantId);
                await _publishEndpoint.Publish(new RecalculateGoldEvent { TenantId = tenantId });
                return true;
            default:
                return false;
        }
    }
}
