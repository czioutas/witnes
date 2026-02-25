namespace Api.Product.PageLoads.Models;

/// <summary>
/// Detailed model for a page load including waterfall and jank data
/// </summary>
public class PageLoadDetailModel
{
    /// <summary>
    /// Unique identifier for this page load (Silver layer ID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Navigation type: LOAD or SPA_NAV
    /// </summary>
    public string EventType { get; set; } = "LOAD";

    /// <summary>
    /// User ID who triggered this page load
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// URL of the page
    /// </summary>
    public string Url { get; set; } = null!;

    /// <summary>
    /// When the page load occurred
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Largest Contentful Paint in milliseconds
    /// </summary>
    public int LcpMs { get; set; }

    /// <summary>
    /// Cumulative Layout Shift score
    /// </summary>
    public decimal Cls { get; set; }

    /// <summary>
    /// Average Time to First Byte in milliseconds
    /// </summary>
    public int AvgTtfbMs { get; set; }

    /// <summary>
    /// Network waterfall - list of resources loaded
    /// </summary>
    public List<ResourceTimingModel> Waterfall { get; set; } = new();

    /// <summary>
    /// Jank reports - performance issues detected
    /// </summary>
    public List<string> JankReports { get; set; } = new();

    /// <summary>
    /// First Contentful Paint in milliseconds (hard nav only, 0 for SPA nav)
    /// </summary>
    public int FcpMs { get; set; }

    /// <summary>
    /// DOM Interactive in milliseconds (hard nav only, 0 for SPA nav)
    /// </summary>
    public int DomInteractiveMs { get; set; }

    /// <summary>
    /// Why data collection stopped (idle, spa_nav, timeout, pagehide)
    /// </summary>
    public string? FinalizeReason { get; set; }

    /// <summary>
    /// Whether this record is incomplete (user navigated away before load settled)
    /// </summary>
    public bool Incomplete { get; set; }

    /// <summary>
    /// Silver ID of the parent hard nav (for SPA navs linking back to their origin)
    /// </summary>
    public Guid? ParentPageLoadId { get; set; }

    /// <summary>
    /// Whether this hard nav has SPA nav children (shell resources were reused)
    /// </summary>
    public bool HasSpaChildren { get; set; }

    /// <summary>
    /// Silver ID of the previous page load in session (for prev/next navigation)
    /// </summary>
    public Guid? PreviousPageLoadId { get; set; }

    /// <summary>
    /// Silver ID of the next page load in session (for prev/next navigation)
    /// </summary>
    public Guid? NextPageLoadId { get; set; }

    /// <summary>
    /// URL of the previous page load (for tooltip display)
    /// </summary>
    public string? PreviousUrl { get; set; }

    /// <summary>
    /// URL of the next page load (for tooltip display)
    /// </summary>
    public string? NextUrl { get; set; }
}

/// <summary>
/// Model for a single resource in the network waterfall
/// </summary>
public class ResourceTimingModel
{
    public int Id { get; set; }
    public string Label { get; set; } = null!;
    public string FullUrl { get; set; } = null!;
    public string Protocol { get; set; } = null!;
    public int StalledMs { get; set; }
    public int TtfbMs { get; set; }
    public int TotalMs { get; set; }
    public string SizeFormatted { get; set; } = null!;
    public bool IsCompressed { get; set; }
}
