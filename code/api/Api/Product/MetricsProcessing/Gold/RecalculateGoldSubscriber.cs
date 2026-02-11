using Api.Application.Tenancy.Services;
using MassTransit;

namespace Api.Product.MetricsProcessing.Gold;

public class RecalculateGoldSubscriber : IConsumer<RecalculateGoldEvent>
{
    private readonly IGoldService _goldService;
    private readonly IRequestTenant _requestTenant;
    private readonly ILogger<RecalculateGoldSubscriber> _logger;

    public RecalculateGoldSubscriber(
        IGoldService goldService,
        IRequestTenant requestTenant,
        ILogger<RecalculateGoldSubscriber> logger)
    {
        _goldService = goldService;
        _requestTenant = requestTenant;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RecalculateGoldEvent> context)
    {
        var msg = context.Message;
        _requestTenant.SetTenantId(msg.TenantId);

        try
        {
            await _goldService.RecalculateAllForTenantAsync(msg.TenantId);
            _logger.LogInformation("Gold recalculation complete for TenantId={TenantId}", msg.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating Gold layer for TenantId={TenantId}", msg.TenantId);
            throw;
        }
    }
}
