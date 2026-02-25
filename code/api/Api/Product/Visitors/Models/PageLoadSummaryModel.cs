using Api.Product.MetricsProcessing.Gold;
using Libs.Domain;

namespace Api.Product.Visitors.Models;

/// <summary>
/// Summary model for a single page load event
/// </summary>
public class PageLoadSummaryModel
{
    // --- Identifiers ---
    public Guid Id { get; set; }
    public Guid SilverId { get; set; }
    public string EventType { get; set; } = "LOAD";
    public string UserId { get; set; } = null!;
    public string UrlPath { get; set; } = null!;

    // --- Environment (UI Icons) ---
    public string DeviceIcon { get; set; } = null!;
    public string BrowserIcon { get; set; } = null!;

    // --- Time & Duration (The "When" and "How Long") ---
    public DateTimeOffset PageRequestedAtByVisitor { get; set; }
    public int TotalInitialLoadMs { get; set; } // The primary "Wait" duration
    public int? SettledTimeMs { get; set; }      // Total duration until idle (for SPAs)

    // --- Status Flags ---
    public bool Incomplete { get; set; }
    public bool IsBadExperience { get; set; }
    public bool UserLeftEarly { get; set; }

    // --- The 4 Pillar "Fault" Toggles ---
    public bool IsBackendIssue { get; set; }
    public bool IsNetworkIssue { get; set; }
    public bool IsPayloadIssue { get; set; }
    public bool IsFrontendIssue { get; set; }

    // --- The 5 Pillar Confidence Scores (0-100) ---
    public int BackendConfidence { get; set; }
    public int NetworkConfidence { get; set; }
    public int PayloadConfidence { get; set; }
    public int FrontendConfidence { get; set; }

    // --- The Raw Reasons (Enums) ---
    public BackendReason[] BackendReasons { get; set; } = [];
    public NetworkReason[] NetworkReasons { get; set; } = [];
    public PayloadReason[] PayloadReasons { get; set; } = [];
    public FrontendReason[] FrontendReasons { get; set; } = [];
    public ExperienceSymptom[] ExperienceSymptoms { get; set; } = [];

    // --- Performance Markers ---
    public string ConnectionQuality { get; set; } = "Good";
    public int AbsoluteLcpMs { get; set; }
    public decimal ClsScore { get; set; }
    public int TotalJankCount { get; set; }
    public int InteractionDeadZoneMs { get; set; }

    // --- Session Stitching ---
    public string? SessionId { get; set; }
    public string? SessionRef { get; set; }

    // --- Overall Verdict ---
    public OverallSentiment OverallSentiment { get; set; }
    public HistoricalComparison HistoricalComparison { get; set; }
}
