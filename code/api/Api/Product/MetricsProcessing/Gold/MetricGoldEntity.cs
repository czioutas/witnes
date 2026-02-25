using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Tenancy.Entities;
using Libs.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Product.MetricsProcessing.Gold;

[Table("metrics_gold")]
[Index(nameof(UserId), nameof(PageRequestedAtByVisitor))]
[Index(nameof(NavigationId))]
[Index(nameof(SessionId))]
public class MetricGoldEntity : TenantAwareEntity
{
    public Guid SilverId { get; set; }
    public string? UserId { get; set; }
    public string? GuestId { get; set; }
    public string UrlPath { get; set; } = null!;
    public DateTimeOffset PageRequestedAtByVisitor { get; set; }

    // --- SPA Navigation Support ---
    public string EventType { get; set; } = "LOAD"; // LOAD or SPA_NAV
    public string? NavigationId { get; set; }
    public string? ParentNavigationId { get; set; }

    // --- Session Stitching ---
    public string? SessionId { get; set; }  // NavigationId of the first LOAD in the session
    public string? SessionRef { get; set; } // Referrer URL from session.ref

    // Connection Quality (from Network Pillar)
    public string ConnectionQuality { get; set; } = "Good";

    // Raw Connection Data for UI display
    public string EffectiveType { get; set; } = "4g";
    public int Rtt { get; set; }
    public decimal DownlinkInMbs { get; set; }

    // 6. Data Completeness
    public bool Incomplete { get; set; }

    // Environment (For Icons)
    public string DeviceIcon { get; set; } = "Desktop";
    public string BrowserIcon { get; set; } = "Chrome";

    // --- The Backend Pillar ---
    public bool IsBackendIssue { get; set; }
    public int BackendConfidence { get; set; } // 0-100
    public BackendReason[] BackendReasons { get; set; } = [];

    // --- The Network Pillar ---
    public bool IsNetworkIssue { get; set; }
    public int NetworkConfidence { get; set; } // 0-100
    public NetworkReason[] NetworkReasons { get; set; } = [];

    // --- The Payload Pillar ---
    public bool IsPayloadIssue { get; set; }
    public int PayloadConfidence { get; set; } // 0-100
    public PayloadReason[] PayloadReasons { get; set; } = [];

    // --- Global Timing Anchor ---
    public int TotalInitialLoadMs { get; set; } // RequestedAt -> LoadEvent

    // --- The Frontend Pillar (The "Who") ---
    public bool IsFrontendIssue { get; set; }
    public int FrontendConfidence { get; set; } // 0-100
    public FrontendReason[] FrontendReasons { get; set; } = [];

    // --- The Experience Pillar (The "What") ---
    public bool IsBadExperience { get; set; } // The ultimate "Was it a failure?" flag
    public ExperienceSymptom[] ExperienceSymptoms { get; set; } = [];

    // --- Normalized Performance Markers (For UI) ---
    public int AbsoluteLcpMs { get; set; }
    public int? SettledTimeMs { get; set; }
    public decimal ClsScore { get; set; }
    public int TotalJankCount { get; set; }
    public int InteractionDeadZoneMs { get; set; }

    // --- Overall Verdict ---
    public OverallSentiment OverallSentiment { get; set; } = OverallSentiment.Neutral;
    public HistoricalComparison HistoricalComparison { get; set; } = HistoricalComparison.InsufficientData;
}