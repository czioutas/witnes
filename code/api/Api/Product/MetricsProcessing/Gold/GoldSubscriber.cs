using Api.Application.Tenancy.Services;
using Api.Data;
using Api.Product.MetricsProcessing.Silver;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Api.Product.MetricsProcessing.Gold;

public class GoldSubscriber : IConsumer<SilverProcessedEvent>
{
    private readonly ApplicationDbContext _context;
    private readonly IRequestTenant _requestTenant;
    private readonly ILogger<GoldSubscriber> _logger;

    public GoldSubscriber(
        ApplicationDbContext context,
        IRequestTenant requestTenant,
        ILogger<GoldSubscriber> logger)
    {
        _context = context;
        _requestTenant = requestTenant;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SilverProcessedEvent> context)
    {
        var msg = context.Message;
        _requestTenant.SetTenantId(msg.TenantId);

        // 1. PULL from the database using the SilverId
        // We only select the columns we need for the Triage view (Gold)
        var silverData = await _context.MetricsSilver
            .Where(s => s.Id == msg.SilverId && s.TenantId == msg.TenantId)
            .Select(s => new
            {
                s.UserId,
                s.Url,
                s.Timestamp,
                s.LcpMs,
                s.Cls,
                s.AvgTtfbMs
            }).FirstOrDefaultAsync();

        if (silverData == null)
        {
            _logger.LogWarning("Silver record {SilverId} not found. Gold cannot be created.", msg.SilverId);
            return;
        }

        // 2. APPLY "Triage" Logic
        // Gold represents the "Verdict" for the CS Agent
        var goldEntity = new MetricGoldEntity
        {
            SilverId = msg.SilverId,
            UserId = silverData.UserId,
            Url = silverData.Url,
            Timestamp = silverData.Timestamp,
            TenantId = msg.TenantId,
            LcpMs = silverData.LcpMs,
            Cls = silverData.Cls,

            // The "Verdict"
            IsSlow = silverData.LcpMs > 2500 || silverData.Cls > 0.1m || silverData.AvgTtfbMs > 400,
            HealthGrade = silverData.LcpMs < 2500 ? "Good" : "Poor"
        };

        // 3. SAVE to the high-speed Gold table
        _context.MetricsGold.Add(goldEntity);
        await _context.SaveChangesAsync();
    }
}