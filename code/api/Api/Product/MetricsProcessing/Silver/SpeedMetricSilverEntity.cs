using Api.Application.Tenancy.Entities;

namespace Api.Product.MetricsProcessing.Silver;

public class MetricSilverEntity : TenantAwareEntity
{
    public Guid BronzeId { get; set; }
    public string UserId { get; set; } = null!;
    public string SessionId { get; set; } = null!;
    public string Url { get; set; } = null!;
    public DateTime Timestamp { get; set; }

    // --- Core Vitals for Triage ---
    public decimal LcpMs { get; set; }
    public decimal Cls { get; set; } // Added for visual stability tracking
    public int AvgTtfbMs { get; set; }

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