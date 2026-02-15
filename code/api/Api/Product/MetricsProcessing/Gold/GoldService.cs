using Api.Data;
using Api.Product.MetricsProcessing.Aggregates;
using Api.Product.MetricsProcessing.Silver;
using Libs.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Product.MetricsProcessing.Gold;

public interface IGoldService
{
    Task<MetricGoldEntity> ProcessFromSilverAsync(Guid silverId, Guid tenantId);

    /// <summary>
    /// Deletes Gold metrics older than the specified cutoff date for a tenant.
    /// </summary>
    Task<int> DeleteOlderThanAsync(Guid tenantId, DateTimeOffset cutoffDate);

    /// <summary>
    /// Deletes all Gold metrics for a tenant.
    /// </summary>
    Task<int> DeleteAllForTenantAsync(Guid tenantId);

    /// <summary>
    /// Recalculates all Gold metrics for a tenant by re-transforming from Silver.
    /// </summary>
    Task RecalculateAllForTenantAsync(Guid tenantId);
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

        // Fetch historical baselines for sentiment + comparison
        var userId = silver.UserId ?? silver.GuestId;
        var pagePath = ExtractPath(silver.Url);

        if (userId != null)
        {
            var stats = await _context.UserPageStats
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.UserId == userId && s.PagePath == pagePath)
                .ToListAsync();

            var last10 = stats.FirstOrDefault(s => s.WindowType == "last_10");
            var last50 = stats.FirstOrDefault(s => s.WindowType == "last_50");
            var last200 = stats.FirstOrDefault(s => s.WindowType == "last_200");

            goldEntity.HistoricalComparison = ComputeHistoricalComparison(silver, last10, last50, last200);
            goldEntity.OverallSentiment = ComputeOverallSentiment(goldEntity);
        }

        _context.MetricsGold.Add(goldEntity);
        await _context.SaveChangesAsync();

        return goldEntity;
    }

    /// <inheritdoc/>
    public async Task<int> DeleteOlderThanAsync(Guid tenantId, DateTimeOffset cutoffDate)
    {
        return await _context.MetricsGold
            .IgnoreQueryFilters()
            .Where(m => m.PageRequestedAtByVisitor < cutoffDate)
            .ExecuteDeleteAsync();
    }

    /// <inheritdoc/>
    public async Task<int> DeleteAllForTenantAsync(Guid tenantId)
    {
        return await _context.MetricsGold
            .Where(m => m.TenantId == tenantId)
            .ExecuteDeleteAsync();
    }

    /// <inheritdoc/>
    public async Task RecalculateAllForTenantAsync(Guid tenantId)
    {
        _logger.LogInformation("Recalculating all Gold metrics for TenantId={TenantId}", tenantId);

        var deleted = await DeleteAllForTenantAsync(tenantId);
        _logger.LogInformation("Deleted {Count} existing Gold records for TenantId={TenantId}", deleted, tenantId);

        var silverRecords = await _context.MetricsSilver
            .Where(s => s.TenantId == tenantId)
            .ToListAsync();

        _logger.LogInformation("Found {Count} Silver records to reprocess for TenantId={TenantId}", silverRecords.Count, tenantId);

        foreach (var silver in silverRecords)
        {
            var goldEntity = TransformToGold(silver);
            goldEntity.TenantId = tenantId;
            _context.MetricsGold.Add(goldEntity);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Gold recalculation complete for TenantId={TenantId}", tenantId);
    }

    public static MetricGoldEntity TransformToGold(MetricSilverEntity silver)
    {
        // Background tab adjustment: browser defers painting until tab is visible,
        // inflating LCP and settling times with idle wait time. Subtract it out.
        int bgOffset = silver.WasBackgroundTab ? silver.BackgroundDurationMs : 0;
        int effectiveLcpMs = Math.Max(0, silver.AbsoluteLcpMs - bgOffset);
        int? effectiveSettledMs = silver.SettledTimeMs.HasValue
            ? Math.Max(effectiveLcpMs, silver.SettledTimeMs.Value - bgOffset)
            : null;

        var connection = AnalyzeConnection(silver);
        var backend = AnalyzeBackend(silver);
        var payload = AnalyzePayload(silver);
        var frontend = AnalyzeFrontend(silver);
        var experience = AnalyzeExperience(silver, effectiveLcpMs);

        return new MetricGoldEntity
        {
            SilverId = silver.Id,
            UserId = silver.UserId,
            GuestId = silver.GuestId,
            UrlPath = ExtractPath(silver.Url),
            PageRequestedAtByVisitor = silver.PageRequestedAtByVisitor,

            // --- 1. Backend Pillar ---
            IsBackendIssue = backend.IsIssue,
            BackendConfidence = backend.Confidence,
            BackendReasons = backend.Reasons,

            // --- 2. Connection Pillar ---
            IsNetworkIssue = connection.IsIssue,
            NetworkConfidence = connection.Confidence,
            NetworkReasons = connection.Reasons,
            ConnectionQuality = connection.Quality,
            EffectiveType = silver.EffectiveType,
            Rtt = silver.Rtt,
            DownlinkInMbs = silver.DownlinkInMbs,

            // --- 3. Payload Pillar ---
            IsPayloadIssue = payload.IsIssue,
            PayloadConfidence = payload.Confidence,
            PayloadReasons = payload.Reasons,

            // --- 4. Frontend Pillar ---
            IsFrontendIssue = frontend.IsIssue,
            FrontendConfidence = frontend.Confidence,
            FrontendReasons = frontend.Reasons,

            // --- 5. Experience Pillar ---
            IsBadExperience = experience.IsIssue,
            ExperienceSymptoms = experience.Symptoms,

            // --- Normalized Data for UI (effective values, adjusted for background tabs) ---
            AbsoluteLcpMs = effectiveLcpMs,
            SettledTimeMs = effectiveSettledMs,
            ClsScore = silver.CumulativeLayoutShift,
            TotalJankCount = silver.TotalJankCount,
            InteractionDeadZoneMs = silver.InteractionDeadZoneMs,

            Incomplete = silver.Incomplete,
            DeviceIcon = silver.DeviceType,
            BrowserIcon = silver.BrowserName,
            TotalInitialLoadMs = silver.InitialDocTtfbMs + (silver.Waterfall?.Any() == true ? silver.Waterfall.Max(x => x.TotalMs) : 0)
        };
    }

    private static (bool IsIssue, ExperienceSymptom[] Symptoms) AnalyzeExperience(MetricSilverEntity silver, int effectiveLcpMs)
    {
        var symptoms = new List<ExperienceSymptom>();

        // 1. Loading Speed (The Anchor) — uses effective LCP (adjusted for background tabs)
        if (effectiveLcpMs > 2500) symptoms.Add(ExperienceSymptom.SlowVisualLoad);

        // 2. Visual Stability
        if (silver.CumulativeLayoutShift > 0.1m) symptoms.Add(ExperienceSymptom.UnstableLayout);

        // 3. Smoothness
        if (silver.TotalJankCount > 5) symptoms.Add(ExperienceSymptom.StutteringUI);

        // 4. The "Zombie UI" (Functional vs Technical)
        if (silver.InteractionDeadZoneMs > 1500) symptoms.Add(ExperienceSymptom.DelayedFunctionalReady);

        // 5. Termination Issues — only flag if the page wasn't already usable.
        // A fast page that didn't "settle" because of a trailing background request
        // or because the user simply navigated away is not a bad experience.
        if (silver.SettledTimeMs == null)
        {
            bool pageWasUsable = effectiveLcpMs <= 2500
                && silver.InteractionDeadZoneMs <= 1500;

            if (!pageWasUsable)
            {
                if (silver.FinalizeReason == "timeout") symptoms.Add(ExperienceSymptom.InfiniteWaterfall);
                if (silver.FinalizeReason == "pagehide") symptoms.Add(ExperienceSymptom.RageQuit);
            }
        }

        return (symptoms.Any(), symptoms.ToArray());
    }

    private static (bool IsIssue, int Confidence, FrontendReason[] Reasons) AnalyzeFrontend(MetricSilverEntity silver)
    {
        var reasons = new List<FrontendReason>();
        var confidenceScores = new List<int>();

        // 1. The "Heavy App" Check (Shell to Content)
        if (silver.ShellToContentGapMs > 1500)
        {
            bool isHighLatency = (silver.Rtt > 400); // Using the flat property from Silver
            bool isWaitingForBigAssets = silver.TotalPayloadBytes > 2 * 1024 * 1024; // > 2MB

            if (!isHighLatency && !isWaitingForBigAssets)
            {
                reasons.Add(FrontendReason.HeavyAppBundle);
                int gapConfidence = Math.Clamp(silver.ShellToContentGapMs / 30, 50, 100);
                confidenceScores.Add(gapConfidence);
            }
        }

        // 2. The "Zombie UI" Check (Interaction Dead Zone)
        // If there is a massive gap between Technical and Functional interactive
        if (silver.InteractionDeadZoneMs > 1200)
        {
            reasons.Add(FrontendReason.ZombieUI);
            confidenceScores.Add(80);
        }

        // 3. Hydration/Frozen UI Check
        // If the main thread was locked for a long time specifically after LCP
        if (silver.FrozenUiGapMs > 1000)
        {
            reasons.Add(FrontendReason.HydrationLockup);
            confidenceScores.Add(70);
        }

        // 4. Implementation Quality (Layout Shift)
        // CLS is almost always a frontend implementation issue (missing aspect ratios, etc.)
        if (silver.CumulativeLayoutShift > 0.15m)
        {
            reasons.Add(FrontendReason.ImplementationLayoutShift);
            confidenceScores.Add(90);
        }

        // 5. Unoptimized Boot Sequence
        // If we have jank (long tasks) specifically happening before the page is usable
        int bootJanks = silver.JankReports?.Count(j => j.S < silver.TechnicalInteractiveMs) ?? 0;
        if (bootJanks > 3)
        {
            reasons.Add(FrontendReason.UnoptimizedBootSequence);
            confidenceScores.Add(60);
        }

        int finalConfidence = confidenceScores.Any() ? confidenceScores.Max() : 0;
        return (reasons.Any(), finalConfidence, reasons.ToArray());
    }

    // =========================================================================
    // Backend Analyzer
    // =========================================================================
    //
    // Key change: We CHECK whether the network explains the slowness
    // before blaming the backend. If third-party is also slow, we reduce
    // backend confidence because the "slow TTFB" might just be the pipe.
    // =========================================================================

    private static (bool IsIssue, int Confidence, BackendReason[] Reasons) AnalyzeBackend(MetricSilverEntity silver)
    {
        var reasons = new List<BackendReason>();
        var confidenceScores = new List<int>();

        bool networkExplainsSlowness = silver.ThirdPartyAlsoSlow
            || silver.DownloadThroughputLow
            || silver.NavigatorReportsBadConnection;

        // ---------------------------------------------------------------
        // PRIMARY: Slow API calls (absolute, always works)
        // SlowestApiTtfbMs = max TotalMs across all FETCH/XHR entries.
        // TotalMs (duration) is always accurate, even cross-origin.
        // ---------------------------------------------------------------
        if (silver.SlowestApiTtfbMs > 1000)
        {
            reasons.Add(BackendReason.SlowCriticalApi);
            int conf = Math.Clamp(silver.SlowestApiTtfbMs / 50, 70, 100);
            conf = networkExplainsSlowness ? Math.Clamp(conf - 15, 40, 80) : conf;
            confidenceScores.Add(conf);
        }
        else if (silver.MeanCriticalApiTtfbMs > 500)
        {
            reasons.Add(BackendReason.SlowCriticalApi);
            int conf = Math.Clamp(silver.MeanCriticalApiTtfbMs / 10, 50, 95);
            conf = networkExplainsSlowness ? Math.Clamp(conf - 15, 30, 70) : conf;
            confidenceScores.Add(conf);
        }

        // ---------------------------------------------------------------
        // Slow initial HTML document
        // ---------------------------------------------------------------
        if (silver.InitialDocTtfbMs > 400)
        {
            reasons.Add(BackendReason.SlowInitialDocument);
            int conf = Math.Clamp(silver.InitialDocTtfbMs / 10, 40, 100);
            conf = networkExplainsSlowness ? Math.Clamp(conf - 20, 20, 60) : conf;
            confidenceScores.Add(conf);
        }

        // ---------------------------------------------------------------
        // Only same-origin is slow (third-party is fine → server issue)
        // ---------------------------------------------------------------
        if (silver.OnlySameOriginSlow)
        {
            if (!reasons.Contains(BackendReason.SlowInitialDocument))
                reasons.Add(BackendReason.SlowInitialDocument);
            confidenceScores.Add(networkExplainsSlowness ? 50 : 90);
        }

        // ---------------------------------------------------------------
        // Heavy API response payloads
        // ---------------------------------------------------------------
        if (silver.MeanCriticalApiTransferMs > 1000 && silver.CriticalApisHaveLargePayloads)
        {
            reasons.Add(BackendReason.HeavyResponsePayload);
            confidenceScores.Add(50);
        }

        // ---------------------------------------------------------------
        // Server Processing Gap (TTFB - RTT > 300ms)
        // ---------------------------------------------------------------
        if (silver.Rtt > 0 && silver.InitialDocTtfbMs - silver.Rtt > 300)
        {
            reasons.Add(BackendReason.ServerProcessingGap);
            int gapConf = Math.Clamp((silver.InitialDocTtfbMs - silver.Rtt) / 10, 40, 95);
            gapConf = networkExplainsSlowness ? Math.Clamp(gapConf - 20, 20, 70) : gapConf;
            confidenceScores.Add(gapConf);
        }

        int finalConfidence = confidenceScores.Any() ? confidenceScores.Max() : 0;
        return (reasons.Any(), finalConfidence, reasons.ToArray());
    }

    private static (bool IsIssue, int Confidence, string Quality, NetworkReason[] Reasons) AnalyzeConnection(MetricSilverEntity silver)
    {
        var reasons = new List<NetworkReason>();
        var confidenceScores = new List<int>();

        // ---------------------------------------------------------------
        // Signal 1: Cross-origin corroboration (STRONGEST signal)
        // If third-party resources are ALSO slow, the pipe is the problem.
        // ---------------------------------------------------------------
        if (silver.ThirdPartyAlsoSlow)
        {
            reasons.Add(NetworkReason.HighLatency);
            // Higher confidence with more third-party samples
            int crossOriginConf = silver.ThirdPartyCount >= 5 ? 95 : silver.ThirdPartyCount >= 3 ? 85 : 70;
            confidenceScores.Add(crossOriginConf);
        }

        // ---------------------------------------------------------------
        // Signal 2: Download throughput is low across the board
        // ---------------------------------------------------------------
        if (silver.DownloadThroughputLow)
        {
            reasons.Add(NetworkReason.LowBandwidth);
            confidenceScores.Add(80);
        }

        // ---------------------------------------------------------------
        // Signal 3: Navigator.connection corroboration
        // ---------------------------------------------------------------
        if (silver.NavigatorReportsBadConnection)
        {
            if (silver.EffectiveType is "2g" or "slow-2g")
            {
                reasons.Add(NetworkReason.UnstableCellular);
                confidenceScores.Add(100);
            }
            else if (silver.EffectiveType == "3g")
            {
                reasons.Add(NetworkReason.UnstableCellular);
                confidenceScores.Add(85);
            }
            else if (silver.Rtt > 400)
            {
                if (!reasons.Contains(NetworkReason.HighLatency))
                    reasons.Add(NetworkReason.HighLatency);
                confidenceScores.Add(75);
            }
        }

        // ---------------------------------------------------------------
        // Signal 4: Browser queuing / congestion
        // ---------------------------------------------------------------
        if (silver.TotalCriticalStalledMs > 500)
        {
            reasons.Add(NetworkReason.NetworkCongestion);
            confidenceScores.Add(80);
        }

        // ---------------------------------------------------------------
        // Signal 5: CDN Baseline Verdict (control measurement)
        // If the tracker's own CDN load is elevated, the network is suspect.
        // ---------------------------------------------------------------
        if (silver.CdnBaselineVerdict is "elevated" or "high")
        {
            reasons.Add(NetworkReason.ElevatedCdnBaseline);
            int cdnConf = silver.CdnBaselineVerdict == "high" ? 85 : 60;
            confidenceScores.Add(cdnConf);
        }

        // ---------------------------------------------------------------
        // Signal 6: Baseline TTFB fallback (when no third-party data)
        // Less reliable on its own — cap confidence lower.
        // ---------------------------------------------------------------
        if (!silver.HasThirdPartyData && silver.BaselineNetworkTtfbMs > 300)
        {
            reasons.Add(NetworkReason.HighLatency);
            int latencyConf = Math.Clamp(silver.BaselineNetworkTtfbMs / 10, 30, 70);
            confidenceScores.Add(latencyConf);
        }

        // --- Quality rating ---
        string quality;
        if (silver.HasThirdPartyData)
        {
            quality = silver.ThirdPartyMedianTtfbMs switch
            {
                <= 100 => "Good",
                <= 250 => "Fair",
                _ => "Poor"
            };
        }
        else
        {
            quality = silver.BaselineNetworkTtfbMs switch
            {
                <= 100 => "Good",
                <= 250 => "Fair",
                _ => "Poor"
            };
        }

        int finalConfidence = confidenceScores.Any() ? confidenceScores.Max() : 0;
        return (reasons.Any(), finalConfidence, quality, reasons.ToArray());
    }

    // --- Payload Analyzer: "Fast Response / Slow Transfer" ---
    private static (bool IsIssue, int Confidence, PayloadReason[] Reasons) AnalyzePayload(MetricSilverEntity silver)
    {
        var reasons = new List<PayloadReason>();
        var confidenceScores = new List<int>();

        if (silver.TotalJsBytes > 1.2 * 1024 * 1024) // 1.2MB Threshold
        {
            reasons.Add(PayloadReason.ScriptBloat);
            confidenceScores.Add(85);
        }

        if (silver.LargeApiCount > 0)
        {
            reasons.Add(PayloadReason.LargeApiResponses);
            // If they have multiple "heavy" APIs, confidence is near 100%
            int apiConf = Math.Clamp(silver.LargeApiCount * 30, 60, 100);
            confidenceScores.Add(apiConf);
        }

        // 1. Absolute Page Weight (The 2MB+ Threshold)
        if (silver.TotalPayloadBytes > 2 * 1024 * 1024)
        {
            reasons.Add(PayloadReason.ExcessiveSize);
            confidenceScores.Add(70);
        }

        // 2. Specific API Bloat (The sophistication we just added)
        // If the APIs are fast to start but slow to finish because they are huge.
        if (silver.CriticalApisHaveLargePayloads && !reasons.Contains(PayloadReason.LargeApiResponses))
        {
            reasons.Add(PayloadReason.LargeApiResponses);
            confidenceScores.Add(80);
        }

        // 3. Protocol Bottleneck (The Architectural FYI)
        // This isn't about "size" in bytes, but "congestion" due to old protocols.
        if (silver.Protocol == "http/1.1" && silver.MaxConcurrentRequests > 6)
        {
            reasons.Add(PayloadReason.ConcurrencyBottleneck); // Better name for the enum
            confidenceScores.Add(60);
        }

        // 4. Compression Check (Quick win evidence)
        // If you have a high count of uncompressed assets in Silver.
        if (silver.UncompressedCriticalAssetsCount > 0)
        {
            reasons.Add(PayloadReason.UncompressedAssets);
            confidenceScores.Add(90);
        }

        int finalConfidence = confidenceScores.Any() ? confidenceScores.Max() : 0;
        return (reasons.Any(), finalConfidence, reasons.ToArray());
    }

    private static HistoricalComparison ComputeHistoricalComparison(
        MetricSilverEntity silver,
        UserPageStatsEntity? last10,
        UserPageStatsEntity? last50,
        UserPageStatsEntity? last200)
    {
        // Not enough data to compare — need at least 5 visits in last_10
        if (last10 == null || last10.VisitCount < 5)
            return HistoricalComparison.InsufficientData;

        // Primary comparison: current LCP vs last_10 average
        var baseline = last10.AvgLoadTimeMs;
        if (baseline <= 0) return HistoricalComparison.InsufficientData;

        var currentLcp = (double)silver.AbsoluteLcpMs;
        var ratio = currentLcp / baseline;

        if (ratio > 1.20) return HistoricalComparison.Worse;
        if (ratio < 0.80) return HistoricalComparison.Better;
        return HistoricalComparison.Similar;
    }

    private static OverallSentiment ComputeOverallSentiment(MetricGoldEntity gold)
    {
        var symptomCount = gold.ExperienceSymptoms.Length;

        // Severe symptoms always Bad
        if (gold.ExperienceSymptoms.Contains(ExperienceSymptom.RageQuit)
            || gold.ExperienceSymptoms.Contains(ExperienceSymptom.InfiniteWaterfall))
            return OverallSentiment.Bad;

        // 3+ symptoms = Bad
        if (symptomCount >= 3)
            return OverallSentiment.Bad;

        // No symptoms + history same/better = Good
        if (symptomCount == 0
            && gold.HistoricalComparison is HistoricalComparison.Similar
                or HistoricalComparison.Better
                or HistoricalComparison.InsufficientData)
            return OverallSentiment.Good;

        // 1-2 minor symptoms OR historical worse
        if (symptomCount <= 2 || gold.HistoricalComparison == HistoricalComparison.Worse)
            return OverallSentiment.Neutral;

        return OverallSentiment.Neutral;
    }

    private static string ExtractPath(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var path = uri.AbsolutePath;
            return path == "/" ? "/" : path.TrimEnd('/');
        }
        return url.Split('?')[0];
    }
}