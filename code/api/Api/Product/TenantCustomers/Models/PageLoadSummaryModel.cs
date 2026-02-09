using Libs.Domain;

namespace Api.Product.TenantCustomers.Models;

/// <summary>
/// Summary model for a single page load event
/// </summary>
public class PageLoadSummaryModel
{
    public Guid Id { get; set; }
    public string SessionId { get; set; } = null!;
    public string? GuestId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string UrlPath { get; set; } = null!;

    // LCP
    public int LcpMs { get; set; }
    public string LcpVerdict { get; set; } = null!;

    // CLS
    public decimal ClsScore { get; set; }
    public string ClsVerdict { get; set; } = null!;

    // Connection
    public bool IsConnectionFault { get; set; }
    public string ConnectionQuality { get; set; } = null!;
    public ConnectionReason[] ConnectionReasons { get; set; } = [];
    public string EffectiveType { get; set; } = null!;
    public int Rtt { get; set; }
    public decimal Downlink { get; set; }

    // Backend
    public int TtfbMs { get; set; }
    public bool IsBackendFault { get; set; }

    // Frontend
    public bool IsFrontendFault { get; set; }

    // Environment
    public string DeviceIcon { get; set; } = null!;
    public string BrowserIcon { get; set; } = null!;

    // Data Completeness
    public bool Incomplete { get; set; }

    public Guid SilverId { get; set; }
}
