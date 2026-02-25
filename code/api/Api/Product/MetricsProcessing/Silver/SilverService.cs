using Api.Data;
using Api.Product.Ingestion.Models;
using Api.Product.MetricsProcessing.Bronze;
using Libs.Domain;
using Microsoft.EntityFrameworkCore;

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

    /// <summary>
    /// Recalculates all Silver metrics for a tenant by re-processing from Bronze.
    /// Returns the IDs of newly created Silver records.
    /// </summary>
    Task RecalculateAllForTenantAsync(Guid tenantId);
}

public class SilverService : ISilverService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SilverService> _logger;

    public SilverService(ApplicationDbContext context, ILogger<SilverService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MetricSilverEntity> ProcessFromBronzeAsync(Guid bronzeId, Guid tenantId)
    {
        _logger.LogDebug("Starting Silver transformation for BronzeId: {BronzeId}", bronzeId);

        var bronze = await _context.MetricsBronze
            .FirstOrDefaultAsync(x => x.Id == bronzeId && x.TenantId == tenantId) ?? throw new InvalidOperationException($"Bronze record {bronzeId} not found.");

        // ---------------------------------------------------------
        // 1. Data Extraction & Contextual Setup
        // ---------------------------------------------------------
        var metadata = bronze.Metadata;
        var performance = bronze.Performance;
        var network = bronze.Network;
        var device = bronze.Device;

        var waterfall = performance.Waterfall ?? [];
        var initialLoad = performance.InitialLoad;
        var jankList = performance.Jank ?? new List<JankMetric>();
        var isSpaNav = metadata?.Event == "SPA_NAV";

        // Server-side CLS delta: sum all clsEvents for this navigation
        // For LOAD events, all shifts belong to this nav.
        // For SPA_NAV events, the tracker already sends only post-route-change shifts
        // via the global accumulator, but we sum them all here regardless.
        var clsEvents = performance.ClsEvents ?? [];
        var cumulativeLayoutShift = clsEvents.Sum(e => e.V);

        // Server-side Incomplete derivation:
        // A record is incomplete when loadEventFiredAt is null AND:
        //   - For SPA_NAV: finalizeReason == 'spa_nav' means cut short by another route change
        //   - For LOAD: finalizeReason != 'idle' means the page never settled
        var incomplete = metadata?.LoadEventFiredAt == null
            && metadata?.FinalizeReason is not "idle";

        // Compute Network Baselines (skip for SPA navs — no initial document fetch)
        var baselineNetworkTtfb = isSpaNav ? 0 : CalculateNetworkBaseline(waterfall);
        var initialDocTtfb = isSpaNav ? 0 : (initialLoad?.Latency?.Ttfb ?? 0);

        // ---------------------------------------------------------
        // 2. Map Silver Entity (The Flat Record)
        // ---------------------------------------------------------
        var silverEntity = new MetricSilverEntity
        {
            Id = Guid.NewGuid(),
            BronzeId = bronzeId,
            TenantId = tenantId,
            UserId = bronze.UserId,
            GuestId = bronze.UserId == null ? bronze.HashId : null,
            Url = bronze.Url,
            PageRequestedAtByVisitor = metadata!.PageRequestedAtByVisitor,
            PageLoadDurationMs = metadata?.PageLoadDurationMs ?? 0,
            Incomplete = incomplete,
            FinalizeReason = metadata?.FinalizeReason,
            IdentifyDelayMs = metadata?.IdentifyDelayMs ?? 0,

            // SPA Navigation Support
            EventType = metadata?.Event ?? "LOAD",
            NavigationId = metadata?.NavigationId,
            ParentNavigationId = metadata?.ParentNavigationId,

            // Session Stitching
            SessionRef = bronze.Session?.Ref,

            WasBackgroundTab = metadata?.WasBackgroundTab ?? false,
            BackgroundDurationMs = metadata?.TabVisibleAtMs ?? 0,

            // Network Context
            EffectiveType = network?.EffectiveType ?? "unknown",
            Rtt = ParseIntMetric(network?.Rtt, "ms"),
            DownlinkInMbs = network?.DownlinkInMbs ?? 0,
            Protocol = isSpaNav ? "spa" : (initialLoad?.Protocol ?? "h2"),

            // Device Context
            UserAgent = device?.UserAgent ?? "Unknown",
            BrowserName = UserAgentParser.GetBrowser(device?.UserAgent),
            DeviceType = UserAgentParser.GetDeviceType(device?.UserAgent),

            // Waterfall Processing
            Waterfall = [.. waterfall.Select(r => new ProcessedResource
            {
                StartMs = r.Start ?? 0,
                Label = $"{(r.Initiator ?? "OTHER").ToUpper()} | {r.Name ?? "Unknown"}",
                FullUrl = r.FullUrl ?? "N/A",
                Protocol = r.Protocol ?? "N/A",
                StalledMs = r.Latency?.Stalled ?? 0,
                TtfbMs = r.Latency?.Ttfb ?? 0,
                TotalMs = r.Latency?.Total ?? 0,
                SizeBytes = r.Data?.TransferInBytes ?? 0,
                IsCompressed = r.Data?.IsCompressed ?? true, // unknown = assume compressed (avoid false positives for cross-origin)
                SameOrigin = r.SameOrigin,
                TimingRestricted = r.TimingRestricted,
                DownloadMs = r.Latency?.DownloadMs ?? 0,
                DownloadBytesPerSec = r.Data?.DownloadBytesPerSec,
                TransferInBytes = r.Data?.TransferInBytes ?? 0
            })],

            // Jank Storage
            JankReports = jankList,

            // Infrastructure & Efficiency Baseline
            BaselineNetworkTtfbMs = baselineNetworkTtfb,
            CdnBaselineTtfbMs = isSpaNav ? 0 : (performance.CdnBaseline?.TtfbMs ?? 0),
            InitialDocTtfbMs = initialDocTtfb,
            ServerEfficiencyRatio = isSpaNav ? 1.0 : (baselineNetworkTtfb > 0 ? (double)initialDocTtfb / baselineNetworkTtfb : 1.0),
            TotalCriticalStalledMs = CalculateTotalCriticalStalled(waterfall),
            MaxConcurrentRequests = CalculateMaxConcurrency(waterfall),
            // ---------------------------------------------------------
            // 3. Frontend Symptoms (The "What Happened?")
            // ---------------------------------------------------------
            AbsoluteLcpMs = performance.Vitals.WebVitals.Lcp,
            FcpMs = isSpaNav ? 0 : performance.Vitals.WebVitals.Fcp,
            CumulativeLayoutShift = cumulativeLayoutShift,
            TotalJankCount = jankList.Count,

            // Settled Time: Null if we didn't finish cleanly
            SettledTimeMs = metadata?.FinalizeReason == "idle"
                ? metadata.PageLoadDurationMs
                : null,

            // ---------------------------------------------------------
            // 4. Interactive Duo & Functional Logic
            // ---------------------------------------------------------
            TechnicalInteractiveMs = isSpaNav ? 0 : performance.Vitals.PageLoad.Interactive
        };

        // Find the last long task that effectively extended the "Hydration/Init" phase
        // We look for tasks starting near TechnicalInteractive and up to 3s after.
        // Skip for SPA navs — no hydration phase.
        if (!isSpaNav)
        {
            var hydrationJank = silverEntity.JankReports
                .Where(j => j.S >= silverEntity.TechnicalInteractiveMs - 500 &&
                            j.S <= silverEntity.TechnicalInteractiveMs + 3000)
                .OrderByDescending(j => (j.S ?? 0) + (j.D ?? 0))
                .FirstOrDefault();

            silverEntity.FunctionalInteractiveMs = hydrationJank != null
                ? (hydrationJank.S ?? 0) + (hydrationJank.D ?? 0)
                : silverEntity.TechnicalInteractiveMs;
        }

        // ---------------------------------------------------------
        // 5. Fault Evidence (The Calculated Gaps)
        // ---------------------------------------------------------
        // For SPA navs, these gaps are meaningless (FCP=0, Interactive=0) — leave at 0.
        if (!isSpaNav)
        {
            silverEntity.ShellToContentGapMs = Math.Max(0, silverEntity.AbsoluteLcpMs - silverEntity.FcpMs);
            silverEntity.InteractionDeadZoneMs = silverEntity.FunctionalInteractiveMs - silverEntity.TechnicalInteractiveMs;
            silverEntity.InteractiveMs = silverEntity.TechnicalInteractiveMs;
            silverEntity.FrozenUiGapMs = Math.Max(0, silverEntity.TechnicalInteractiveMs - silverEntity.AbsoluteLcpMs);
        }

        var resources = silverEntity.Waterfall;

        silverEntity.TotalPayloadBytes = resources.Sum(r => r.SizeBytes);

        // Bucket the bytes by looking at the Label (which we set to Initiator) or Extension
        silverEntity.TotalJsBytes = resources
            .Where(r => r.Label.Contains("SCRIPT") || r.FullUrl.Contains(".js"))
            .Sum(r => r.SizeBytes);

        silverEntity.TotalImgBytes = resources
            .Where(r => r.Label.Contains("IMG") || r.Label.Contains("IMAGE") || IsImageExtension(r.FullUrl))
            .Sum(r => r.SizeBytes);

        silverEntity.TotalApiBytes = resources
            .Where(r => r.Label.Contains("FETCH") || r.Label.Contains("XMLHTTPREQUEST"))
            .Sum(r => r.SizeBytes);

        // Implementation Fault: Find individual APIs that are way too large for a single request
        silverEntity.LargeApiCount = resources
            .Count(r => (r.Label.Contains("FETCH") || r.Label.Contains("XMLHTTPREQUEST")) && r.SizeBytes > 200 * 1024);

        // CSS bytes
        silverEntity.TotalCssBytes = resources
            .Where(r => r.Label.Contains("LINK") || r.FullUrl.EndsWith(".css"))
            .Sum(r => r.SizeBytes);

        // Compression Fault: Text-based assets that aren't compressed (only flag if >10KB — tiny assets don't matter)
        silverEntity.UncompressedCriticalAssetsCount = resources
            .Count(r => !r.IsCompressed && r.SizeBytes > 10 * 1024 && (r.Label.Contains("SCRIPT") || r.Label.Contains("LINK") || r.Label.Contains("FETCH")));

        // ---------------------------------------------------------
        // 6. Critical API Signals
        // ---------------------------------------------------------
        // For timing-restricted (cross-origin) entries, TtfbMs=0 is meaningless.
        // Use TotalMs (duration) as the effective response time — it's still accurate.
        static int EffectiveApiTime(ProcessedResource r) =>
            r.TimingRestricted ? r.TotalMs : r.TtfbMs;

        silverEntity.MeanCriticalApiTtfbMs = CalculateMeanCriticalApiTtfb(waterfall);

        var criticalApis = resources
            .Where(r => r.Label.Contains("FETCH") || r.Label.Contains("XMLHTTPREQUEST"))
            .OrderBy(r => r.StalledMs + EffectiveApiTime(r)) // order by start-ish time
            .Take(5)
            .ToList();

        if (criticalApis.Count != 0)
        {
            // Mean transfer time (download phase) for critical APIs
            silverEntity.MeanCriticalApiTransferMs = (int)criticalApis
                .Where(r => !r.TimingRestricted)
                .DefaultIfEmpty()
                .Average(r => r != null ? Math.Max(0, r.TotalMs - r.TtfbMs - r.StalledMs) : 0);

            // Do any critical APIs have large payloads (>100KB)?
            silverEntity.CriticalApisHaveLargePayloads = criticalApis
                .Any(r => r.SizeBytes > 100 * 1024);

            // Mean TTFB for small-payload APIs (pure server logic speed)
            var smallApis = criticalApis.Where(r => r.SizeBytes <= 100 * 1024).ToList();
            silverEntity.SmallApiMeanTtfbMs = smallApis.Any()
                ? (int)smallApis.Average(r => EffectiveApiTime(r))
                : 0;

            // How many critical APIs started within 50ms of each other?
            silverEntity.ConcurrentCriticalRequestCount = CountConcurrentStarts(criticalApis, 50);
        }

        // Helper for image detection
        bool IsImageExtension(string url) =>
            url.EndsWith(".png") || url.EndsWith(".jpg") || url.EndsWith(".jpeg") || url.EndsWith(".webp") || url.EndsWith(".svg");

        // Network fingerprint still valuable for SPA navs (XHR/fetch resources give signal)
        silverEntity = BuildNetworkFingerprint(bronze, silverEntity);

        // ---------------------------------------------------------
        // 7. Page Sentiment Signal (Industry Verdict)
        // ---------------------------------------------------------
        silverEntity.IndustryVerdict = silverEntity.AbsoluteLcpMs switch
        {
            < 2500 => IndustryVerdict.Fast,
            < 4000 => IndustryVerdict.Normal,
            < 6000 => IndustryVerdict.Slow,
            _ => IndustryVerdict.Critical
        };

        // ---------------------------------------------------------
        // 8. CDN Baseline Verdict
        // ---------------------------------------------------------
        silverEntity.CdnBaselineVerdict = isSpaNav ? "unknown" : silverEntity.CdnBaselineTtfbMs switch
        {
            0 => "unknown",   // No CDN data available
            <= 50 => "normal",
            <= 150 => "elevated",
            _ => "high"
        };

        // ---------------------------------------------------------
        // 9. Frontend: JS execution as % of total load
        // ---------------------------------------------------------
        var totalJankDurationMs = silverEntity.JankReports.Sum(j => j.D ?? 0);
        var loadDuration = silverEntity.PageLoadDurationMs > 0 ? silverEntity.PageLoadDurationMs : silverEntity.AbsoluteLcpMs;
        silverEntity.JsDurationPctOfLoad = loadDuration > 0
            ? Math.Round((double)totalJankDurationMs / loadDuration * 100, 1)
            : 0;

        // ---------------------------------------------------------
        // 10. Backend: Slowest individual API call (by total duration — always accurate)
        // ---------------------------------------------------------
        var allApis = resources
            .Where(r => r.Label.Contains("FETCH") || r.Label.Contains("XMLHTTPREQUEST"))
            .ToList();
        if (allApis.Count != 0)
        {
            var slowest = allApis.OrderByDescending(r => r.TotalMs).First();
            silverEntity.SlowestApiTtfbMs = slowest.TotalMs;
            silverEntity.SlowestApiUrl = slowest.FullUrl;
        }

        // ---------------------------------------------------------
        // 11. Session Stitching — assign SessionId at ingestion time
        // ---------------------------------------------------------
        silverEntity.SessionId = await ResolveSessionIdAsync(silverEntity, tenantId);

        await _context.MetricsSilver.AddAsync(silverEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Silver record saved successfully: {SilverId}", silverEntity.Id);

        return silverEntity;
    }

    // =========================================================================
    // Network Fingerprint: The foundation for differential diagnosis
    // =========================================================================
    //
    // The fingerprint answers: "What does this user's pipe look like?"
    // by comparing same-origin vs third-party resource behavior.
    //
    // Key insight:
    //   - Slow network  → BOTH same-origin and third-party are slow
    //   - Slow server   → Only same-origin is slow, third-party is fine
    //   - Slow download → download throughput (bytes/sec) is low everywhere
    //   - Slow backend  → high TTFB (wait) but normal download throughput
    // =========================================================================

    private static MetricSilverEntity BuildNetworkFingerprint(MetricBronzeEntity bronze, MetricSilverEntity silver)
    {
        var waterfall = silver.Waterfall ?? new List<ProcessedResource>();

        // Partition resources — exclude timing-restricted entries from TTFB analysis
        // (cross-origin without Timing-Allow-Origin have TtfbMs=0 which is meaningless)
        var sameOrigin = waterfall.Where(w => w.SameOrigin).ToList();
        var thirdParty = waterfall.Where(w => !w.SameOrigin && !w.TimingRestricted).ToList();

        // Calculate median TTFB for each group (more robust than mean against outliers)
        var sameOriginMedianTtfb = MedianOrDefault(sameOrigin.Select(w => w.TtfbMs).ToList());
        var thirdPartyMedianTtfb = MedianOrDefault(thirdParty.Select(w => w.TtfbMs).ToList());

        // Calculate median download throughput (bytes/sec) for each group
        // Only consider resources with meaningful download (>10ms, >0 bytes)
        var sameOriginThroughputs = sameOrigin
            .Where(w => w.DownloadMs > 10 && w.TransferInBytes > 0)
            .Select(w => (int)(w.TransferInBytes / (w.DownloadMs / 1000)))
            .ToList();

        var thirdPartyThroughputs = thirdParty
            .Where(w => w.DownloadMs > 10 && w.TransferInBytes > 0)
            .Select(w => (int)(w.TransferInBytes / (w.DownloadMs / 1000)))
            .ToList();

        var sameOriginMedianThroughput = MedianOrDefault(sameOriginThroughputs);
        var thirdPartyMedianThroughput = MedianOrDefault(thirdPartyThroughputs);

        // All throughputs combined — the "pipe speed" for this session
        var allThroughputs = sameOriginThroughputs.Concat(thirdPartyThroughputs).ToList();
        var overallMedianThroughput = MedianOrDefault(allThroughputs);

        // Does the browser's own assessment (navigator.connection) agree?
        bool navigatorReportsBadConnection =
            silver.EffectiveType is "2g" or "3g" or "slow-2g"
            || silver.Rtt > 400
            || (silver.DownlinkInMbs > 0 && silver.DownlinkInMbs < 2.0m);

        bool navigatorReportsGoodConnection =
            silver.EffectiveType == "4g"
            && silver.Rtt < 100
            && silver.DownlinkInMbs > 10.0m;

        // --- The Differential Diagnosis ---

        bool hasThirdPartyData = thirdParty.Count >= 2;
        bool thirdPartyAlsoSlow = hasThirdPartyData && thirdPartyMedianTtfb > 200;
        bool onlySameOriginSlow = hasThirdPartyData && sameOriginMedianTtfb > 300 && thirdPartyMedianTtfb < 150;

        // Download throughput check: is the pipe itself slow?
        // Below ~500KB/s is a genuinely constrained connection
        bool downloadThroughputLow = overallMedianThroughput > 0 && overallMedianThroughput < 500_000;

        // "High TTFB but normal download" = server think-time, not network
        bool highWaitNormalDownload = sameOriginMedianTtfb > 300
            && sameOriginMedianThroughput > 500_000
            && !downloadThroughputLow;

        silver.SameOriginCount = sameOrigin.Count;
        silver.ThirdPartyCount = waterfall.Count(w => !w.SameOrigin); // All third-party, including restricted
        silver.HasThirdPartyData = hasThirdPartyData;

        silver.SameOriginMedianTtfbMs = (int)sameOriginMedianTtfb;
        silver.ThirdPartyMedianTtfbMs = (int)thirdPartyMedianTtfb;

        silver.SameOriginMedianThroughputBps = sameOriginMedianThroughput;
        silver.ThirdPartyMedianThroughputBps = thirdPartyMedianThroughput;
        silver.OverallMedianThroughputBps = overallMedianThroughput;

        silver.ThirdPartyAlsoSlow = thirdPartyAlsoSlow;
        silver.OnlySameOriginSlow = onlySameOriginSlow;
        silver.DownloadThroughputLow = downloadThroughputLow;
        silver.HighWaitNormalDownload = highWaitNormalDownload;

        silver.NavigatorReportsBadConnection = navigatorReportsBadConnection;
        silver.NavigatorReportsGoodConnection = navigatorReportsGoodConnection;

        // CDN baseline corroboration: if the tracker script itself was slow,
        // that's strong evidence the network is the bottleneck (it's a tiny file from a CDN)
        if (silver.CdnBaselineTtfbMs > 150)
        {
            silver.ThirdPartyAlsoSlow = true; // CDN is third-party, confirms network issue
        }

        return silver;
    }

    /// <summary>
    /// Resolves the SessionId for a Silver record at ingestion time.
    /// 1. SPA_NAV with ParentNavigationId → inherit SessionId from parent
    /// 2. LOAD with same-domain SessionRef → inherit SessionId from most recent record for same user
    /// 3. Otherwise → new session root, SessionId = own NavigationId
    /// </summary>
    private async Task<string?> ResolveSessionIdAsync(MetricSilverEntity silver, Guid tenantId)
    {
        // Case 1: SPA_NAV — inherit from parent LOAD via ParentNavigationId
        if (!string.IsNullOrEmpty(silver.ParentNavigationId))
        {
            var parentSessionId = await _context.MetricsSilver
                .Where(s => s.TenantId == tenantId && s.NavigationId == silver.ParentNavigationId)
                .Select(s => s.SessionId)
                .FirstOrDefaultAsync();

            if (parentSessionId != null)
                return parentSessionId;
        }

        // Case 2: LOAD with same-domain ref → continues existing session
        if (!string.IsNullOrEmpty(silver.SessionRef) && IsSameDomainRef(silver.SessionRef, silver.Url))
        {
            // Find the most recent Silver record for the same user
            var query = _context.MetricsSilver
                .Where(s => s.TenantId == tenantId && s.PageRequestedAtByVisitor < silver.PageRequestedAtByVisitor);

            if (silver.UserId != null)
                query = query.Where(s => s.UserId == silver.UserId);
            else if (silver.GuestId != null)
                query = query.Where(s => s.GuestId == silver.GuestId);
            else
                return silver.NavigationId; // No user identity — new session

            var previousSessionId = await query
                .OrderByDescending(s => s.PageRequestedAtByVisitor)
                .Select(s => s.SessionId)
                .FirstOrDefaultAsync();

            if (previousSessionId != null)
                return previousSessionId;
        }

        // Case 3: New session root — external ref, direct, or null ref
        return silver.NavigationId;
    }

    /// <summary>
    /// Checks if a referrer URL is from the same domain as the page URL.
    /// </summary>
    private static bool IsSameDomainRef(string refUrl, string pageUrl)
    {
        if (Uri.TryCreate(refUrl, UriKind.Absolute, out var refUri) &&
            Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri))
        {
            return string.Equals(refUri.Host, pageUri.Host, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private static double MedianOrDefault(List<int> values)
    {
        if (values == null || values.Count == 0) return 0;
        var sorted = values.OrderBy(v => v).ToList();
        int mid = sorted.Count / 2;
        return (double)sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }

    /// <inheritdoc/>
    public async Task<int> DeleteOlderThanAsync(Guid tenantId, DateTimeOffset cutoffDate)
    {
        return await _context.MetricsSilver
            .Where(m => m.PageRequestedAtByVisitor < cutoffDate)
            .ExecuteDeleteAsync();
    }

    /// <inheritdoc/>
    public async Task RecalculateAllForTenantAsync(Guid tenantId)
    {
        _logger.LogInformation("Recalculating all Silver metrics for TenantId={TenantId}", tenantId);

        var deleted = await _context.MetricsSilver
            .Where(m => m.TenantId == tenantId)
            .ExecuteDeleteAsync();
        _logger.LogInformation("Deleted {Count} existing Silver records for TenantId={TenantId}", deleted, tenantId);

        var bronzeRecords = await _context.MetricsBronze
            .Where(b => b.TenantId == tenantId)
            .ToListAsync();

        _logger.LogInformation("Found {Count} Bronze records to reprocess for TenantId={TenantId}", bronzeRecords.Count, tenantId);

        foreach (var bronze in bronzeRecords)
        {
            await ProcessFromBronzeAsync(bronze.Id, tenantId);
        }

        _logger.LogInformation("Silver recalculation complete for TenantId={TenantId}", tenantId);
    }

    // --- Helper Parsers to handle the browser strings ---

    private static int ParseIntMetric(string? value, string unit)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        var clean = value.Replace(unit, "").Trim();
        return int.TryParse(clean, out var result) ? result : 0;
    }

    // -----------------------------
    // Calculation Helpers
    // -----------------------------

    /// <summary>
    /// Finds the Mean TTFB of the fastest 3-5 non-cached static assets.
    /// This represents the "Speed of Light" for the user's current connection.
    /// </summary>
    private int CalculateNetworkBaseline(List<WaterfallResource> waterfall)
    {
        var staticAssets = waterfall
            .Where(r => (r.Initiator == "script" || r.Initiator == "link" || r.Initiator == "css" || r.Initiator == "img")
                        && (r.Latency?.Ttfb > 0) // Exclude zeroed cross-origin / fully cached
                        && r.Data?.TransferInBytes > 0) // Ignore cached items
            .OrderBy(r => r.Latency?.Ttfb)
            .Take(5)
            .ToList();

        if (!staticAssets.Any()) return 0;
        return (int)staticAssets.Average(r => r.Latency?.Ttfb ?? 0);
    }

    /// <summary>
    /// Mean total duration of the first 5 XHR/Fetch requests.
    /// Uses Total (duration) which is always accurate, even cross-origin.
    /// </summary>
    private int CalculateMeanCriticalApiTtfb(List<WaterfallResource> waterfall)
    {
        var criticalApis = waterfall
            .Where(r => r.Initiator == "xmlhttprequest" || r.Initiator == "fetch")
            .OrderBy(r => r.Start)
            .Take(5)
            .ToList();

        if (!criticalApis.Any()) return 0;
        return (int)criticalApis.Average(r => r.Latency?.Total ?? 0);
    }

    /// <summary>
    /// Detects how many requests were overlapping in the "In-Flight" state.
    /// </summary>
    private int CalculateMaxConcurrency(List<WaterfallResource> waterfall)
    {
        if (!waterfall.Any()) return 0;

        // Sort all start and end events to find the peak overlap
        var events = waterfall.Select(r => new { Time = r.Start, Type = 1 }) // 1 = Start
            .Concat(waterfall.Select(r => new { Time = r.Start + (r.Latency?.Total ?? 0), Type = -1 })) // -1 = End
            .OrderBy(e => e.Time)
            .ThenBy(e => e.Type);

        int max = 0, current = 0;
        foreach (var e in events)
        {
            current += e.Type;
            if (current > max) max = current;
        }
        return max;
    }

    /// <summary>
    /// Counts how many resources started within a given window (ms) of each other.
    /// </summary>
    private static int CountConcurrentStarts(List<ProcessedResource> resources, int windowMs)
    {
        if (resources.Count < 2) return 0;
        var starts = resources.Select(r => r.StalledMs + r.TtfbMs).OrderBy(s => s).ToList();
        int concurrent = 0;
        for (int i = 1; i < starts.Count; i++)
        {
            if (starts[i] - starts[i - 1] <= windowMs)
                concurrent++;
        }
        return concurrent;
    }

    /// <summary>
    /// Sums the stalled time for the critical Rank 1-5 APIs.
    /// High stalled time usually points to browser queuing (HTTP/1.1) or Network Saturation.
    /// </summary>
    private static int CalculateTotalCriticalStalled(List<WaterfallResource> waterfall)
    {
        return waterfall
            .Where(r => r.Initiator == "xmlhttprequest" || r.Initiator == "fetch")
            .OrderBy(r => r.Start)
            .Take(5)
            .Sum(r => Math.Max(0, r.Latency?.Stalled ?? 0));
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