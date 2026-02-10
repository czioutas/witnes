using Api.Data;
using Api.Product.Ingestion.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Product.MetricsProcessing.Silver;

public interface ISilverService
{
    /// <summary>
    /// Transforms raw Bronze JSON into structured Silver deep-dive data.
    /// </summary>
    Task<MetricSilverEntity> ProcessFromBronzeAsync(Guid bronzeId, Guid tenantId);

    /// <summary>
    /// Deletes Silver metrics older than the specified cutoff date for a tenant.
    /// </summary>
    Task<int> DeleteOlderThanAsync(Guid tenantId, DateTimeOffset cutoffDate);
}

public class SilverService : ISilverService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SilverService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public SilverService(ApplicationDbContext context, ILogger<SilverService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MetricSilverEntity> ProcessFromBronzeAsync(Guid bronzeId, Guid tenantId)
    {
        _logger.LogDebug("Starting Silver transformation for BronzeId: {BronzeId}", bronzeId);

        var bronze = await _context.MetricsBronze
            .FirstOrDefaultAsync(x => x.Id == bronzeId && x.TenantId == tenantId);

        if (bronze == null)
            throw new InvalidOperationException($"Bronze record {bronzeId} not found.");

        // 1. Deserialize the blocks from the Bronze entity
        var metadata = JsonSerializer.Deserialize<MetadataModel>(bronze.Metadata, JsonOptions);
        var performance = JsonSerializer.Deserialize<PerformanceModel>(bronze.Performance, JsonOptions);
        var network = bronze.Network != null ? JsonSerializer.Deserialize<NetworkModel>(bronze.Network, JsonOptions) : null;
        var device = bronze.Device != null ? JsonSerializer.Deserialize<DeviceModel>(bronze.Device, JsonOptions) : null;

        if (performance == null)
            throw new InvalidOperationException("Failed to deserialize Bronze Performance payload.");

        // 2. Construct the Silver Entity
        var silverEntity = new MetricSilverEntity
        {
            Id = Guid.NewGuid(),
            BronzeId = bronzeId,
            TenantId = tenantId,
            UserId = bronze.UserId,
            GuestId = bronze.UserId == null ? bronze.HashId : null,
            Url = bronze.Url,
            PageRequestedAtByVisitor = metadata!.PageRequestedAtByVisitor,
            WTrackerListenerCalledAt = metadata.ListenerCalledAtByEmitEvent,
            IngestionEndpointFiredAt = metadata.CallWitnesAt,
            Incomplete = metadata?.Incomplete ?? false,

            // --- Network Extraction ---
            EffectiveType = network?.EffectiveType ?? "unknown",
            Rtt = ParseIntMetric(network?.Rtt, "ms"),
            Downlink = ParseDecimalMetric(network?.Downlink, "Mb/s"),

            // --- Device Extraction ---
            UserAgent = device?.UserAgent ?? "Unknown",
            // Note: These helpers would use a simple regex or library to get "Chrome"/"Desktop"
            BrowserName = UserAgentParser.GetBrowser(device?.UserAgent),
            DeviceType = UserAgentParser.GetDeviceType(device?.UserAgent),

            // --- Performance & Waterfall ---
            Waterfall = performance.Waterfall?.Select(r => new ProcessedResource
            {
                Label = $"{(r.Initiator ?? "OTHER").ToUpper()} | {r.Name ?? "Unknown"}",
                FullUrl = r.FullUrl ?? "N/A",
                Protocol = r.Protocol ?? "N/A",
                StalledMs = r.Latency?.Stalled ?? 0,
                TtfbMs = r.Latency?.Ttfb ?? 0,
                TotalMs = r.Latency?.Total ?? 0,
                SizeFormatted = r.Data?.Transfer ?? "0B",
                IsCompressed = r.Data?.IsCompressed ?? false
            }).ToList() ?? new List<ProcessedResource>(),

            JankReports = performance.Jank?.Select(j => j.D ?? "Unknown Jank").ToList() ?? new List<string>(),

            LcpMs = ParseDecimalMetric(performance.Vitals?.WebVitals?.Lcp, "ms"),
            Cls = performance.Vitals?.WebVitals?.Cls ?? 0,

            AvgTtfbMs = (performance.Waterfall != null && performance.Waterfall.Any())
                ? (int)performance.Waterfall.Average(w => w.Latency?.Ttfb ?? 0)
                : 0
        };

        await _context.MetricsSilver.AddAsync(silverEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Silver record saved successfully: {SilverId}", silverEntity.Id);
        return silverEntity;
    }

    /// <inheritdoc/>
    public async Task<int> DeleteOlderThanAsync(Guid tenantId, DateTimeOffset cutoffDate)
    {
        return await _context.MetricsSilver
            .Where(m => m.PageRequestedAtByVisitor < cutoffDate)
            .ExecuteDeleteAsync();
    }

    // --- Helper Parsers to handle the browser strings ---

    private static int ParseIntMetric(string? value, string unit)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        var clean = value.Replace(unit, "").Trim();
        return int.TryParse(clean, out var result) ? result : 0;
    }

    private static decimal ParseDecimalMetric(string? value, string unit)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        var clean = value.Replace(unit, "").Trim();
        return decimal.TryParse(clean, out var result) ? result : 0;
    }
}

public static class UserAgentParser
{
    public static string GetBrowser(string? ua)
    {
        if (string.IsNullOrWhiteSpace(ua)) return "Unknown";

        if (ua.Contains("Edg/")) return "Edge";
        if (ua.Contains("Chrome/")) return "Chrome";
        if (ua.Contains("Safari/") && !ua.Contains("Chrome/")) return "Safari";
        if (ua.Contains("Firefox/")) return "Firefox";
        if (ua.Contains("OPR/") || ua.Contains("Opera/")) return "Opera";

        return "Browser";
    }

    public static string GetDeviceType(string? ua)
    {
        if (string.IsNullOrWhiteSpace(ua)) return "Desktop";

        var lowerUa = ua.ToLower();
        if (lowerUa.Contains("mobi") || lowerUa.Contains("android") || lowerUa.Contains("iphone"))
            return "Mobile";

        if (lowerUa.Contains("tablet") || lowerUa.Contains("ipad"))
            return "Tablet";

        return "Desktop";
    }
}