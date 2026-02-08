using Api.Application.Tenancy.Services;
using Api.Product.Ingestion.Events;
using MassTransit;

namespace Api.Product.MetricsProcessing.Bronze;

/// <summary>
/// Subscriber for ingested speed metrics - stores in Bronze layer
/// </summary>
public class BronzeSubscriber : IConsumer<SpeedMetricIngestedEvent>
{
    private readonly IBronzeService _bronzeService;
    private readonly IRequestTenant _requestTenant;
    private readonly ILogger<BronzeSubscriber> _logger;

    public BronzeSubscriber(
        IBronzeService bronzeService,
        IRequestTenant requestTenant,
        ILogger<BronzeSubscriber> logger)
    {
        _bronzeService = bronzeService ?? throw new ArgumentNullException(nameof(bronzeService));
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<SpeedMetricIngestedEvent> context)
    {
        var message = context.Message;

        _requestTenant.SetTenantId(message.TenantId);

        try
        {
            // Store in Bronze layer
            var bronzeEntity = await _bronzeService.StoreAsync(
                message.Event,
                message.TenantId,
                message.IngestedAt);

            // Publish event for Silver processing
            await context.Publish(new BronzeProcessedEvent
            {
                BronzeId = bronzeEntity.Id,
                TenantId = bronzeEntity.TenantId
            });

            _logger.LogInformation(
                "Bronze processing complete for Id={BronzeId}",
                bronzeEntity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Bronze metric for TenantId={TenantId}", message.TenantId);
            throw; // MassTransit will retry on exception
        }
    }
}
