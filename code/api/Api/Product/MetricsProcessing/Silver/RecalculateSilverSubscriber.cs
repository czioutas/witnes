using Api.Application.Tenancy.Services;
using Api.Product.MetricsProcessing.Gold;
using MassTransit;

namespace Api.Product.MetricsProcessing.Silver;

public class RecalculateSilverSubscriber : IConsumer<RecalculateSilverEvent>
{
    private readonly ISilverService _silverService;
    private readonly IGoldService _goldService;
    private readonly IRequestTenant _requestTenant;
    private readonly ILogger<RecalculateSilverSubscriber> _logger;

    public RecalculateSilverSubscriber(
        ISilverService silverService,
        IGoldService goldService,
        IRequestTenant requestTenant,
        ILogger<RecalculateSilverSubscriber> logger)
    {
        _silverService = silverService;
        _goldService = goldService;
        _requestTenant = requestTenant;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RecalculateSilverEvent> context)
    {
        var msg = context.Message;
        _requestTenant.SetTenantId(msg.TenantId);

        try
        {
            // Delete Gold first since it depends on Silver
            await _goldService.DeleteAllForTenantAsync(msg.TenantId);

            // Recalculate Silver from Bronze
            await _silverService.RecalculateAllForTenantAsync(msg.TenantId);

            // Cascade: trigger Gold recalculation from the new Silver records
            await context.Publish(new RecalculateGoldEvent
            {
                TenantId = msg.TenantId
            });

            _logger.LogInformation("Silver recalculation complete for TenantId={TenantId}, Gold recalculation triggered", msg.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating Silver layer for TenantId={TenantId}", msg.TenantId);
            throw;
        }
    }
}
