using Api.Application.Tenancy.Services;
using Api.Product.MetricsProcessing.Silver;
using MassTransit;

namespace Api.Product.MetricsProcessing.Gold;

public class GoldSubscriber : IConsumer<SilverProcessedEvent>
{
    private readonly IGoldService _goldService;
    private readonly IRequestTenant _requestTenant;
    private readonly ILogger<GoldSubscriber> _logger;

    public GoldSubscriber(
        IGoldService goldService,
        IRequestTenant requestTenant,
        ILogger<GoldSubscriber> logger)
    {
        _goldService = goldService;
        _requestTenant = requestTenant;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SilverProcessedEvent> context)
    {
        var msg = context.Message;
        _requestTenant.SetTenantId(msg.TenantId);

        try
        {
            await _goldService.ProcessFromSilverAsync(msg.SilverId, msg.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Gold layer for SilverId={SilverId}", msg.SilverId);
            throw;
        }
    }
}
