using Api.Data;
using Api.Product.MetricsProcessing.Silver;
using Libs.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Product.MetricsProcessing.Gold;

public interface IGoldService
{
    Task<MetricGoldEntity> ProcessFromSilverAsync(Guid silverId, Guid tenantId);
}

public class GoldService : IGoldService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GoldService> _logger;

    public GoldService(ApplicationDbContext context, ILogger<GoldService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MetricGoldEntity> ProcessFromSilverAsync(Guid silverId, Guid tenantId)
    {
        var silver = await _context.MetricsSilver
            .Where(s => s.Id == silverId && s.TenantId == tenantId)
            .FirstOrDefaultAsync();

        if (silver == null) throw new InvalidOperationException($"Silver record {silverId} not found.");

        var goldEntity = TransformToGold(silver);
        goldEntity.TenantId = tenantId;

        _context.MetricsGold.Add(goldEntity);
        await _context.SaveChangesAsync();

        return goldEntity;
    }

    public static MetricGoldEntity TransformToGold(MetricSilverEntity silver)
    {
        var lcpMs = (int)silver.LcpMs;
        var connection = AnalyzeConnection(silver);

        return new MetricGoldEntity
        {
            SilverId = silver.Id,
            UserId = silver.UserId,
            GuestId = silver.GuestId,
            UrlPath = ExtractPath(silver.Url),
            Timestamp = silver.Timestamp,

            // Speed & Stability
            LcpMs = lcpMs,
            LcpVerdict = EvaluateLcp(lcpMs),
            ClsScore = silver.Cls,
            ClsVerdict = silver.Cls <= 0.1m ? "Solid" : "Shifty",

            // Connection Pillar
            IsConnectionFault = connection.IsFault,
            ConnectionQuality = connection.Quality,
            ConnectionReasons = connection.Reasons,
            EffectiveType = silver.EffectiveType,
            Rtt = silver.Rtt,
            Downlink = silver.Downlink,

            // Backend & Frontend Pillars
            TtfbMs = silver.AvgTtfbMs,
            IsBackendFault = EvaluateBackendFault(silver.AvgTtfbMs, lcpMs),
            IsFrontendFault = EvaluateFrontendFault(silver),

            Incomplete = silver.Incomplete,
            DeviceIcon = silver.DeviceType,
            BrowserIcon = silver.BrowserName
        };
    }

    private static (bool IsFault, string Quality, ConnectionReason[] Reasons) AnalyzeConnection(MetricSilverEntity silver)
    {
        var reasons = new List<ConnectionReason>();

        // 1. Latency Check
        if (silver.Rtt > 300) reasons.Add(ConnectionReason.HighLatency);

        // 2. Bandwidth Check
        if (silver.Downlink > 0 && silver.Downlink < 1.5m) reasons.Add(ConnectionReason.LowBandwidth);

        // 3. Congestion/Stall Check
        var avgStalled = silver.Waterfall.Count > 0 ? silver.Waterfall.Average(r => r.StalledMs) : 0;
        if (avgStalled > 150) reasons.Add(ConnectionReason.NetworkCongestion);

        // 4. Signal Quality
        if (silver.EffectiveType is "2g" or "3g") reasons.Add(ConnectionReason.UnstableCellular);

        var quality = avgStalled switch { <= 50 => "Good", <= 150 => "Fair", _ => "Poor" };

        return (reasons.Any(), quality, reasons.ToArray());
    }

    private static string EvaluateLcp(int lcpMs) => lcpMs switch { <= 2500 => "Fast", <= 4000 => "Average", _ => "Slow" };

    private static bool EvaluateBackendFault(int ttfbMs, int lcpMs) => ttfbMs > 600 || (lcpMs > 0 && ttfbMs > lcpMs * 0.5);

    private static bool EvaluateFrontendFault(MetricSilverEntity silver) => silver.JankReports.Count > 0 || silver.Waterfall.Count > 50;

    private static string ExtractPath(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var path = uri.AbsolutePath;
            return path == "/" ? "Home" : path.TrimEnd('/');
        }
        return url.Split('?')[0];
    }
}