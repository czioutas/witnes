namespace Api.Product.Metrics.Models;

/// <summary>
/// Response model for speed metric data
/// </summary>
public class SpeedMetricResponse
{
    public Guid Id { get; set; }
    public Guid SilverId { get; set; }
    public string UserId { get; set; } = null!;
    public string SessionId { get; set; } = null!;
    public string UrlPath { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }

    public int LcpMs { get; set; }
    public string LcpVerdict { get; set; } = null!;
    public decimal ClsScore { get; set; }
    public string ClsVerdict { get; set; } = null!;

    public bool IsConnectionFault { get; set; }
    public string ConnectionQuality { get; set; } = null!;
    public int TtfbMs { get; set; }
    public bool IsBackendFault { get; set; }
    public bool IsFrontendFault { get; set; }

    public string DeviceIcon { get; set; } = null!;
    public string BrowserIcon { get; set; } = null!;

    public bool Incomplete { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
