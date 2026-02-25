using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Tenancy.Entities;
using Api.Product.Ingestion.Models;
using Libs.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Product.MetricsProcessing.Silver;

[Table("metrics_silver")]
[Index(nameof(NavigationId))]
[Index(nameof(UserId), nameof(PageRequestedAtByVisitor))]
[Index(nameof(GuestId), nameof(PageRequestedAtByVisitor))]
public class MetricSilverEntity : TenantAwareEntity
{
    public Guid BronzeId { get; set; }
    public string? UserId { get; set; }
    public string? GuestId { get; set; }
    public string Url { get; set; } = null!;
    public DateTimeOffset PageRequestedAtByVisitor { get; set; }
    public int PageLoadDurationMs { get; set; }
    public int EmittedAt { get; set; }
    public string? FinalizeReason { get; set; }
    public int IdentifyDelayMs { get; set; }

    // --- SPA Navigation Support ---
    public string EventType { get; set; } = "LOAD"; // LOAD or SPA_NAV
    public string? NavigationId { get; set; }
    public string? ParentNavigationId { get; set; }

    // --- Session Stitching ---
    public string? SessionId { get; set; }  // NavigationId of the first LOAD in the session
    public string? SessionRef { get; set; } // Referrer URL from session.ref

    // --- NEW: Connection Context (Missing from your snippet) ---
    public int Rtt { get; set; }               // Round-trip time (latency)
    public decimal DownlinkInMbs { get; set; }      // Bandwidth in Mbps
    public string EffectiveType { get; set; } = "4g"; // '4g', '3g', etc.

    // --- NEW: Device Data ---
    public string UserAgent { get; set; } = null!;
    public string DeviceType { get; set; } = "Desktop";
    public string BrowserName { get; set; } = "Chrome";

    // --- Metadata ---
    public bool Incomplete { get; set; }

    // --- Background Tab Detection ---
    // When a page opens in a background tab, the browser defers painting.
    // LCP and settling times get inflated with idle wait time.
    public bool WasBackgroundTab { get; set; }
    public int BackgroundDurationMs { get; set; } // Time the tab was hidden before becoming visible

    // --- The Detailed Waterfall ---
    public List<ProcessedResource> Waterfall { get; set; } = [];
    public List<JankMetric> JankReports { get; set; } = [];

    // --- The Infrastructure Baseline ---
    public string Protocol { get; set; } = "h2"; // h1.1, h2, h3
    public int BaselineNetworkTtfbMs { get; set; } // Mean of fastest 3-5 statics
    public int CdnBaselineTtfbMs { get; set; } // Tracker script's own TTFB (CDN control measurement)

    // --- The Implementation Evidence ---
    public int InitialDocTtfbMs { get; set; }
    public int MeanCriticalApiTtfbMs { get; set; }

    // --- The Concurrency "Cleaner" ---
    // Total time requests spent waiting for a socket (higher in HTTP/1.1)
    public int TotalCriticalStalledMs { get; set; }

    // --- The Calculated Ratios ---
    // (InitialDocTtfbMs / BaselineNetworkTtfbMs)
    public double ServerEfficiencyRatio { get; set; }

    // --- Concurrency Evidence ---
    // How many of the Rank 1-5 APIs started within 50ms of each other?
    public int ConcurrentCriticalRequestCount { get; set; }

    // The maximum number of requests that were "In-Flight" at the same time
    public int MaxConcurrentRequests { get; set; }

    // How many of the Rank 1-5 APIs had a large payload (>100KB)?
    public int LargeApiCount { get; set; }

    // Mean TTFB specifically for APIs that had SMALL payloads
    // This is your "Pure Logic" speed.
    public int SmallApiMeanTtfbMs { get; set; }

    // Mean Transfer Time (Total - TTFB - Stalled) for the large APIs
    public int MeanCriticalApiTransferMs { get; set; } // "The Download" (Payload)
    public bool CriticalApisHaveLargePayloads { get; set; }

    // --- Data Evidence ---
    public long TotalPayloadBytes { get; set; }

    // --- Compression Evidence ---
    public int UncompressedCriticalAssetsCount { get; set; }

    // --- Page Sentiment Signal ---
    public IndustryVerdict IndustryVerdict { get; set; } = IndustryVerdict.Fast;

    // --- CDN Baseline Verdict ---
    public string CdnBaselineVerdict { get; set; } = "normal"; // "normal" / "elevated" / "high"

    // --- Frontend: JS execution as % of total load ---
    public double JsDurationPctOfLoad { get; set; }

    // --- Backend: Slowest individual API call ---
    public int SlowestApiTtfbMs { get; set; }
    public string? SlowestApiUrl { get; set; }

    // 1. Symptoms (What happened?)
    public int AbsoluteLcpMs { get; set; }           // The Primary Clock
    public int? SettledTimeMs { get; set; }          // Nullable based on FinalizeReason == 'idle'
    public decimal CumulativeLayoutShift { get; set; } // Visual Jitter
    public int TotalJankCount { get; set; }          // Count of LongTasks

    // 2. Fault Evidence (The "Why")
    public int FcpMs { get; set; }                   // The "Shell" Arrival
    public int ShellToContentGapMs { get; set; }     // (LCP - FCP) -> Heavy App
    public int InteractiveMs { get; set; }           // Responsive point
    public int FrozenUiGapMs { get; set; }           // (Interactive - LCP) -> Hydration

    // The "Technical" marker (Browser event: domInteractive)
    public int TechnicalInteractiveMs { get; set; }

    // The "Actual" marker (TechnicalInteractive + trailing Long Tasks)
    public int FunctionalInteractiveMs { get; set; }

    // The "Frustration Gap" (Functional - Technical)
    // High value = The page was visible but "dead" to user input
    public int InteractionDeadZoneMs { get; set; }

    // --- The Volume Pillar (The "What is heavy?") ---
    public long TotalJsBytes { get; set; }          // Script Bloat
    public long TotalImgBytes { get; set; }         // Media Weight
    public long TotalCssBytes { get; set; }         // Styling Weight
    public long TotalApiBytes { get; set; }         // Data Weight (JSON)

    public int SameOriginCount { get; set; }
    public int ThirdPartyCount { get; set; }
    public bool HasThirdPartyData { get; set; }

    public int SameOriginMedianTtfbMs { get; set; }
    public int ThirdPartyMedianTtfbMs { get; set; }

    public double SameOriginMedianThroughputBps { get; set; }
    public double ThirdPartyMedianThroughputBps { get; set; }
    public double OverallMedianThroughputBps { get; set; }

    /// <summary>Third-party resources are also slow → network is the bottleneck.</summary>
    public bool ThirdPartyAlsoSlow { get; set; }

    /// <summary>Only same-origin is slow, third-party is fine → server issue.</summary>
    public bool OnlySameOriginSlow { get; set; }

    /// <summary>Download throughput is low everywhere → pipe is constrained.</summary>
    public bool DownloadThroughputLow { get; set; }

    /// <summary>High TTFB but normal download speed → server think-time.</summary>
    public bool HighWaitNormalDownload { get; set; }

    /// <summary>navigator.connection reports bad network.</summary>
    public bool NavigatorReportsBadConnection { get; set; }

    /// <summary>navigator.connection reports good network.</summary>
    public bool NavigatorReportsGoodConnection { get; set; }
}

public class ProcessedResource
{
    public int Id { get; set; }
    public int StartMs { get; set; }
    public string Label { get; set; } = null!;
    public string FullUrl { get; set; } = null!;
    public string Protocol { get; set; } = null!;
    public int StalledMs { get; set; }
    public int TtfbMs { get; set; }
    public int TotalMs { get; set; }  // Fixed name
    public long SizeBytes { get; set; }
    public bool IsCompressed { get; set; }
    public bool SameOrigin { get; set; }
    public bool TimingRestricted { get; set; }
    public decimal DownloadMs { get; set; }
    public long? DownloadBytesPerSec { get; set; }
    public long TransferInBytes { get; set; }
}