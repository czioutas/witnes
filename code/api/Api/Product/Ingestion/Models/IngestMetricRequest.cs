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
    [property: JsonPropertyName("event")][Required] string Event,
    [property: JsonPropertyName("pk")][Required] string Pk,
    [property: JsonPropertyName("ts")] DateTime? Ts,
    [property: JsonPropertyName("incomplete")] bool Incomplete
);

public record SessionModel(
    [property: JsonPropertyName("userId")] string? UserId,
    [property: JsonPropertyName("url")][Required] string Url,
    [property: JsonPropertyName("ref")] string? Ref
);

public record PerformanceModel(
    [property: JsonPropertyName("vitals")] VitalsModel? Vitals,
    [property: JsonPropertyName("waterfall")] List<WaterfallResource>? Waterfall, // Optional list
    [property: JsonPropertyName("jank")] List<JankMetric>? Jank
);

// Internal Waterfall items are now fully optional to prevent 400 errors
public record WaterfallResource(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("fullUrl")] string? FullUrl,
    [property: JsonPropertyName("initiator")] string? Initiator,
    [property: JsonPropertyName("protocol")] string? Protocol,
    [property: JsonPropertyName("start")] int? Start,
    [property: JsonPropertyName("latency")] LatencyMetrics? Latency,
    [property: JsonPropertyName("data")] DataMetrics? Data
);

public record LatencyMetrics(
    [property: JsonPropertyName("stalled")] int Stalled,
    [property: JsonPropertyName("ttfb")] int Ttfb,
    [property: JsonPropertyName("total")] int Total
);

public record DataMetrics(
    [property: JsonPropertyName("transfer")] string? Transfer,
    [property: JsonPropertyName("isCompressed")] bool? IsCompressed
);

public record VitalsModel(
    [property: JsonPropertyName("pageLoad")] Dictionary<string, string>? PageLoad,
    [property: JsonPropertyName("webVitals")] WebVitals? WebVitals
);

public record WebVitals(
    [property: JsonPropertyName("fcp")] string? Fcp,
    [property: JsonPropertyName("lcp")] string? Lcp,
    [property: JsonPropertyName("cls")] decimal? Cls
);

public record NetworkModel(
    [property: JsonPropertyName("effectiveType")] string? EffectiveType,
    [property: JsonPropertyName("rtt")] string? Rtt,
    [property: JsonPropertyName("downlink")] string? Downlink
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
public record JankMetric([property: JsonPropertyName("d")] string? D);