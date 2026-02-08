using Api.Application.Tenancy.Services;
using Api.Product.MetricsProcessing.Bronze;
using MassTransit;

namespace Api.Product.MetricsProcessing.Silver;

/// <summary>
/// Subscriber for Bronze processed events - processes and stores in Silver layer
/// </summary>
public class SilverSubscriber : IConsumer<BronzeProcessedEvent>
{
    private readonly ISilverService _silverService;
    private readonly IRequestTenant _requestTenant;
    private readonly ILogger<SilverSubscriber> _logger;

    public SilverSubscriber(
        ISilverService silverService,
        IRequestTenant requestTenant,
        ILogger<SilverSubscriber> logger)
    {
        _silverService = silverService ?? throw new ArgumentNullException(nameof(silverService));
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<BronzeProcessedEvent> context)
    {
        var message = context.Message;

        _requestTenant.SetTenantId(message.TenantId);

        try
        {
            // Process and store in Silver layer
            var silverEntity = await _silverService.ProcessFromBronzeAsync(
                message.BronzeId,
                message.TenantId);

            // Publish event for Gold processing
            await context.Publish(new SilverProcessedEvent
            {
                SilverId = silverEntity.Id,
                BronzeId = silverEntity.BronzeId,
                TenantId = silverEntity.TenantId
            });

            _logger.LogInformation(
                "Silver processing complete for Id={SilverId}",
                silverEntity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing Silver layer for BronzeId={BronzeId}",
                message.BronzeId);
            throw; // MassTransit will retry on exception
        }
    }
}
