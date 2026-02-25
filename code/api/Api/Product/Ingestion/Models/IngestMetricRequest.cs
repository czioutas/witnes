using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Api.Product.Ingestion.Models;

// Root is still strict: we need the core objects to exist
public record IngestMetricRequestModel(
    [property: JsonPropertyName("metadata")][Required] MetadataModel Metadata,
    [property: JsonPropertyName("session")][Required] SessionModel Session,
    [property: JsonPropertyName("performance")][Required] PerformanceModel Performance,
    [property: JsonPropertyName("network")] NetworkModel? Network,
    [property: JsonPropertyName("device")] DeviceModel? Device
);

public record MetadataModel(
    [property: JsonPropertyName("durationMs")] int PageLoadDurationMs, // time difference between pageRequestedAtByVisitor and last activity (not including the 2s delay)
    [property: JsonPropertyName("emittedAt")] DateTimeOffset EmittedAt, // when the api call was made
    [property: JsonPropertyName("loadEventFiredAt")] int? LoadEventFiredAt, // when the load event fired (null for SPA navs before load)
    [property: JsonPropertyName("event")][Required] string Event, // LOAD or SPA_NAV
    [property: JsonPropertyName("finalizeReason")] string? FinalizeReason,
    [property: JsonPropertyName("identifyDelayMs")] int IdentifyDelayMs, // Time from pageRequestedAtByVisitor to when identify() was called
    [property: JsonPropertyName("navigationId")] string NavigationId,
    [property: JsonPropertyName("parentNavigationId")] string? ParentNavigationId, // For SPA navs: the initial LOAD's navigationId
    [property: JsonPropertyName("pageRequestedAtByVisitor")] DateTimeOffset PageRequestedAtByVisitor,
    [property: JsonPropertyName("incomplete")] bool Incomplete,
    [property: JsonPropertyName("wasBackgroundTab")] bool WasBackgroundTab,
    [property: JsonPropertyName("tabVisibleAtMs")] int? TabVisibleAtMs,
    [property: JsonPropertyName("pk")][Required] string Pk
);

public record SessionModel(
    [property: JsonPropertyName("userId")] string? UserId,
    [property: JsonPropertyName("url")][Required] string Url,
    [property: JsonPropertyName("ref")] string? Ref
);

public record PerformanceModel(
    [property: JsonPropertyName("vitals")] VitalsModel Vitals,
    [property: JsonPropertyName("initialLoad")] InitialLoadModel? InitialLoad, // null for SPA navs
    [property: JsonPropertyName("cdnBaseline")] CdnBaselineModel? CdnBaseline,
    [property: JsonPropertyName("waterfall")] List<WaterfallResource>? Waterfall,
    [property: JsonPropertyName("clsEvents")] List<ClsEvent>? ClsEvents, // Raw CLS shift events with timestamps
    [property: JsonPropertyName("jank")] List<JankMetric>? Jank
);

public record CdnBaselineModel(
    [property: JsonPropertyName("ttfbMs")] int TtfbMs,
    [property: JsonPropertyName("totalMs")] int TotalMs,
    [property: JsonPropertyName("transferBytes")] long TransferBytes
);

public record InitialLoadModel(
    [property: JsonPropertyName("latency")] LatencyMetrics? Latency,
    [property: JsonPropertyName("data")] DataMetrics? Data,
    [property: JsonPropertyName("protocol")] string? Protocol
);

// Internal Waterfall items are now fully optional to prevent 400 errors
public record WaterfallResource(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("fullUrl")] string? FullUrl,
    [property: JsonPropertyName("initiator")] string? Initiator,
    [property: JsonPropertyName("protocol")] string? Protocol,
    [property: JsonPropertyName("sameOrigin")] bool SameOrigin,
    [property: JsonPropertyName("timingRestricted")] bool TimingRestricted,
    [property: JsonPropertyName("start")] int? Start,
    [property: JsonPropertyName("latency")] LatencyMetrics? Latency,
    [property: JsonPropertyName("data")] DataMetrics? Data
);

public record LatencyMetrics(
    [property: JsonPropertyName("stalled")] int Stalled,
    [property: JsonPropertyName("ttfb")] int Ttfb,
    [property: JsonPropertyName("downloadMs")] decimal DownloadMs,
    [property: JsonPropertyName("total")] int Total
);

public record DataMetrics(
    [property: JsonPropertyName("transferInBytes")] long TransferInBytes,
    [property: JsonPropertyName("downloadBytesPerSec")] long? DownloadBytesPerSec,
    [property: JsonPropertyName("isCompressed")] bool? IsCompressed
);

public record VitalsModel(
    [property: JsonPropertyName("metricsAtIdentify")] MetricsAtIdentify MetricsAtIdentify,
    [property: JsonPropertyName("metricsAtLoad")] MetricsAtLoad MetricsAtLoad,
    [property: JsonPropertyName("pageLoad")] PageLoad PageLoad,
    [property: JsonPropertyName("webVitals")] WebVitals WebVitals
);

public record WebVitals(
    [property: JsonPropertyName("fcp")] int Fcp,
    [property: JsonPropertyName("lcp")] int Lcp
);

public record ClsEvent(
    [property: JsonPropertyName("t")] int T, // Timestamp (ms, relative to performance.timeOrigin)
    [property: JsonPropertyName("v")] decimal V // Shift value
);

public record MetricsAtIdentify(
    [property: JsonPropertyName("lcp")] int Lcp,
    [property: JsonPropertyName("cls")] decimal Cls
);

public record MetricsAtLoad(
    [property: JsonPropertyName("lcp")] int Lcp,
    [property: JsonPropertyName("cls")] decimal Cls
);

public record PageLoad(
    [property: JsonPropertyName("complete")] int Complete,
    [property: JsonPropertyName("interactive")] int Interactive
);


public record NetworkModel(
    [property: JsonPropertyName("effectiveType")] string? EffectiveType,
    [property: JsonPropertyName("rtt")] string? Rtt,
    [property: JsonPropertyName("downlinkInMbs")] decimal DownlinkInMbs
);

public record DeviceModel(
    [property: JsonPropertyName("userAgent")] string? UserAgent,
    [property: JsonPropertyName("platform")] string? Platform,
    [property: JsonPropertyName("language")] string? Language,
    [property: JsonPropertyName("screen")] ScreenSize? Screen,
    [property: JsonPropertyName("viewport")] ViewportSize? Viewport
);

public record ScreenSize(int? W, int? H, decimal? Dpr);
public record ViewportSize(int? W, int? H);
public record JankMetric(
    [property: JsonPropertyName("s")] int? S, // Start time as a number
    [property: JsonPropertyName("d")] int? D  // Duration as a number
);