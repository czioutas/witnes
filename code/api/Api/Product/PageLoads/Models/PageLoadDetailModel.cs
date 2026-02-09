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
    /// User ID who triggered this page load
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Session ID
    /// </summary>
    public string SessionId { get; set; } = null!;

    /// <summary>
    /// URL of the page
    /// </summary>
    public string Url { get; set; } = null!;

    /// <summary>
    /// When the page load occurred
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Largest Contentful Paint in milliseconds
    /// </summary>
    public decimal LcpMs { get; set; }

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
