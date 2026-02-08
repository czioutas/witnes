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

        // 1. Fetch the Raw Bronze Data
        var bronze = await _context.MetricsBronze
            .FirstOrDefaultAsync(x => x.Id == bronzeId && x.TenantId == tenantId);

        if (bronze == null)
        {
            _logger.LogError("Critical: Bronze record {BronzeId} not found.", bronzeId);
            throw new InvalidOperationException($"Bronze record {bronzeId} not found.");
        }

        // 2. Deserialize the "Black Box"
        var performance = JsonSerializer.Deserialize<PerformanceModel>(bronze.RawPerformanceData, JsonOptions);

        if (performance == null)
        {
            _logger.LogWarning("Performance data for {BronzeId} was empty.", bronzeId);
            performance = new PerformanceModel(null, new List<WaterfallResource>(), new List<JankMetric>());
        }

        // 3. Construct the Structured Silver Entity with Null-Safety for Optional Fields
        var silverEntity = new MetricSilverEntity
        {
            Id = Guid.NewGuid(),
            BronzeId = bronzeId,
            TenantId = tenantId,

            // Searchable Identity
            UserId = bronze.UserId,
            SessionId = bronze.SessionId,
            Url = bronze.Url,
            Timestamp = bronze.IngestedAt,

            // --- WATERFALL TRANSFORMATION ---
            // Handles potential nulls from the browser
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

            // --- JANK & VITALS ---
            JankReports = performance.Jank?.Select(j => j.D ?? "Unknown Jank").ToList() ?? new List<string>(),

            LcpMs = ExtractNumeric(performance.Vitals?.WebVitals?.Lcp),
            Cls = performance.Vitals?.WebVitals?.Cls ?? 0,

            AvgTtfbMs = (performance.Waterfall != null && performance.Waterfall.Any())
                ? (int)performance.Waterfall.Average(w => w.Latency?.Ttfb ?? 0)
                : 0
        };

        // 4. Save to Silver Table
        await _context.MetricsSilver.AddAsync(silverEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Silver record saved successfully: {SilverId}", silverEntity.Id);

        return silverEntity;
    }

    private decimal ExtractNumeric(string? value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        var cleanValue = value.Replace("ms", "").Trim();
        return decimal.TryParse(cleanValue, out var result) ? result : 0;
    }
}