using Api.Application.Tenancy.Entities;
using Libs.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Product.MetricsProcessing.Gold;



[Index(nameof(UserId), nameof(Timestamp))]
public class MetricGoldEntity : TenantAwareEntity
{
    public Guid SilverId { get; set; }
    public string UserId { get; set; } = null!;
    public string SessionId { get; set; } = null!;
    public string UrlPath { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }

    // 1. LCP (Speed)
    public int LcpMs { get; set; }
    public string LcpVerdict { get; set; } = "Fast";

    // 2. CLS (Stability)
    public decimal ClsScore { get; set; }
    public string ClsVerdict { get; set; } = "Solid";

    // 3. Connection Pillar
    public bool IsConnectionFault { get; set; }
    public string ConnectionQuality { get; set; } = "Good";
    public ConnectionReason[] ConnectionReasons { get; set; } = [];

    // Raw Connection Data for UI display
    public string EffectiveType { get; set; } = "4g";
    public int Rtt { get; set; }
    public decimal Downlink { get; set; }

    // 4. Backend Pillar
    public int TtfbMs { get; set; }
    public bool IsBackendFault { get; set; }

    // 5. Frontend Pillar
    public bool IsFrontendFault { get; set; }

    // Environment (For Icons)
    public string DeviceIcon { get; set; } = "Desktop";
    public string BrowserIcon { get; set; } = "Chrome";
}