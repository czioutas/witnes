using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Tenancy.Entities;

namespace Api.Product.MetricsProcessing.Silver;

[Table("metrics_silver")]
public class MetricSilverEntity : TenantAwareEntity
{
    public Guid BronzeId { get; set; }
    public string? UserId { get; set; }
    public string? GuestId { get; set; }
    public string Url { get; set; } = null!;
    public DateTimeOffset PageRequestedAtByVisitor { get; set; }
    public DateTimeOffset WTrackerListenerCalledAt { get; set; }
    public DateTimeOffset IngestionEndpointFiredAt { get; set; }

    // --- Core Vitals for Triage ---
    public decimal LcpMs { get; set; }
    public decimal Cls { get; set; } // Added for visual stability tracking
    public int AvgTtfbMs { get; set; }

    // --- NEW: Connection Context (Missing from your snippet) ---
    public int Rtt { get; set; }               // Round-trip time (latency)
    public decimal Downlink { get; set; }      // Bandwidth in Mbps
    public string EffectiveType { get; set; } = "4g"; // '4g', '3g', etc.

    // --- NEW: Device Data ---
    public string UserAgent { get; set; } = null!;
    public string DeviceType { get; set; } = "Desktop";
    public string BrowserName { get; set; } = "Chrome";

    // --- Metadata ---
    public bool Incomplete { get; set; }

    // --- The Detailed Waterfall ---
    public List<ProcessedResource> Waterfall { get; set; } = new();
    public List<string> JankReports { get; set; } = new();
}

public class ProcessedResource
{
    public int Id { get; set; }
    public string Label { get; set; } = null!;
    public string FullUrl { get; set; } = null!;
    public string Protocol { get; set; } = null!;
    public int StalledMs { get; set; }
    public int TtfbMs { get; set; }
    public int TotalMs { get; set; }           // Fixed name
    public string SizeFormatted { get; set; } = null!; // Fixed name
    public bool IsCompressed { get; set; }
}