using Api.Data;
using Api.Product.Ingestion.Models;
using Api.Product.MetricsProcessing.Silver;
using Api.Product.PageLoads.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Product.PageLoads;

/// <summary>
/// Service interface for retrieving page load detail data
/// </summary>
public interface IPageLoadsService
{
    /// <summary>
    /// Gets detailed information for a specific page load from the silver layer
    /// </summary>
    /// <param name="id">The page load ID (silver layer ID)</param>
    /// <returns>Detailed page load model or null if not found</returns>
    Task<PageLoadDetailModel?> GetPageLoadDetailAsync(Guid id);
}

/// <summary>
/// Service for retrieving detailed page load data from Silver layer metrics
/// </summary>
public class PageLoadsService : IPageLoadsService
{
    private readonly ApplicationDbContextRead _contextRead;
    private readonly ILogger<PageLoadsService> _logger;

    public PageLoadsService(
        ApplicationDbContextRead contextRead,
        ILogger<PageLoadsService> logger)
    {
        _contextRead = contextRead ?? throw new ArgumentNullException(nameof(contextRead));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<PageLoadDetailModel?> GetPageLoadDetailAsync(Guid id)
    {
        _logger.LogInformation("Retrieving page load detail for ID: {PageLoadId}", id);

        var silverMetric = await _contextRead.MetricsSilver
            .AsNoTracking()
            .Where(m => m.Id == id)
            .FirstOrDefaultAsync();

        if (silverMetric == null)
        {
            _logger.LogWarning("Page load not found: {PageLoadId}", id);
            return null;
        }

        var detail = new PageLoadDetailModel
        {
            Id = silverMetric.Id,
            EventType = silverMetric.EventType,
            UserId = silverMetric.UserId ?? silverMetric.GuestId ?? "Unknown",
            Url = silverMetric.Url,
            Timestamp = silverMetric.PageRequestedAtByVisitor,
            LcpMs = silverMetric.AbsoluteLcpMs,
            Cls = silverMetric.CumulativeLayoutShift,
            AvgTtfbMs = silverMetric.InitialDocTtfbMs,
            FcpMs = silverMetric.FcpMs,
            DomInteractiveMs = silverMetric.TechnicalInteractiveMs,
            FinalizeReason = silverMetric.FinalizeReason,
            Incomplete = silverMetric.Incomplete,
            Waterfall = silverMetric.Waterfall.Select(r => new ResourceTimingModel
            {
                Id = r.Id,
                Label = r.Label,
                FullUrl = r.FullUrl,
                Protocol = r.Protocol,
                StalledMs = r.StalledMs,
                TtfbMs = r.TtfbMs,
                TotalMs = r.TotalMs,
                IsCompressed = r.IsCompressed,
                SizeFormatted = FormatBytes(r.SizeBytes)
            }).ToList(),
            JankReports = silverMetric.JankReports
                .Where(j => j.S.HasValue && j.D.HasValue)
                .Select(j => FormatJankReport(j, silverMetric.Waterfall))
                .ToList()
        };

        // Resolve parent page load ID for SPA navs
        if (!string.IsNullOrEmpty(silverMetric.ParentNavigationId))
        {
            detail.ParentPageLoadId = await _contextRead.MetricsSilver.AsNoTracking()
                .Where(m => m.NavigationId == silverMetric.ParentNavigationId)
                .Select(m => (Guid?)m.Id)
                .FirstOrDefaultAsync();
        }

        // Check if this hard nav has SPA nav children
        if (silverMetric.EventType == "LOAD" && !string.IsNullOrEmpty(silverMetric.NavigationId))
        {
            detail.HasSpaChildren = await _contextRead.MetricsSilver.AsNoTracking()
                .AnyAsync(m => m.ParentNavigationId == silverMetric.NavigationId);
        }

        // Compute prev/next navigation IDs scoped to session
        var sessionQuery = _contextRead.MetricsSilver.AsNoTracking();
        if (!string.IsNullOrEmpty(silverMetric.SessionId))
        {
            // Scope to same session
            sessionQuery = sessionQuery.Where(m => m.SessionId == silverMetric.SessionId);
        }
        else
        {
            // Fallback: scope to same user
            if (silverMetric.UserId != null)
                sessionQuery = sessionQuery.Where(m => m.UserId == silverMetric.UserId);
            else if (silverMetric.GuestId != null)
                sessionQuery = sessionQuery.Where(m => m.GuestId == silverMetric.GuestId && m.UserId == null);
        }

        var previous = await sessionQuery
            .Where(m => m.PageRequestedAtByVisitor < silverMetric.PageRequestedAtByVisitor)
            .OrderByDescending(m => m.PageRequestedAtByVisitor)
            .Select(m => new { m.Id, m.Url })
            .FirstOrDefaultAsync();

        var next = await sessionQuery
            .Where(m => m.PageRequestedAtByVisitor > silverMetric.PageRequestedAtByVisitor)
            .OrderBy(m => m.PageRequestedAtByVisitor)
            .Select(m => new { m.Id, m.Url })
            .FirstOrDefaultAsync();

        detail.PreviousPageLoadId = previous?.Id;
        detail.PreviousUrl = previous?.Url;
        detail.NextPageLoadId = next?.Id;
        detail.NextUrl = next?.Url;

        _logger.LogInformation(
            "Retrieved page load detail: {PageLoadId}, URL: {Url}, Resources: {ResourceCount}, Jank Reports: {JankCount}",
            id,
            detail.Url,
            detail.Waterfall.Count,
            detail.JankReports.Count);

        return detail;
    }

    private static string FormatJankReport(JankMetric jank, List<ProcessedResource> waterfall)
    {
        var jankStart = jank.S!.Value;
        var jankDuration = jank.D!.Value;
        var baseReport = $"Long task at {jankStart}ms (duration: {jankDuration}ms)";

        // Find scripts whose execution phase overlaps with the jank window.
        // A script executes after download completes: StartMs + StalledMs + TtfbMs + DownloadMs.
        // We look for scripts where download finished shortly before jank started,
        // or that were still in-flight (StartMs + TotalMs covers the jank start).
        var scripts = waterfall
            .Where(r => r.StartMs > 0 && (r.Label.Contains("SCRIPT") || r.FullUrl.EndsWith(".js")))
            .ToList();

        if (scripts.Count == 0)
            return baseReport;

        // Best match: script whose total window (start → start+total) covers the jank start,
        // or whose download completed just before the jank (execution phase).
        var likelyScript = scripts
            .Select(r => new
            {
                Resource = r,
                DownloadEndMs = r.StartMs + r.TotalMs,
                // How close is the download-end to the jank start? Execution follows download.
                ExecutionGap = jankStart - (r.StartMs + r.TotalMs)
            })
            .Where(x =>
                // Script was still loading during jank (overlapping)
                (x.Resource.StartMs <= jankStart && x.DownloadEndMs >= jankStart)
                // Or script finished downloading within 500ms before jank (parse/execute phase)
                || (x.ExecutionGap >= 0 && x.ExecutionGap <= 500))
            .OrderBy(x => Math.Abs(x.ExecutionGap))
            .FirstOrDefault();

        if (likelyScript == null)
            return baseReport;

        // Extract a short filename from the URL
        var url = likelyScript.Resource.FullUrl;
        var fileName = ExtractFileName(url);

        return $"{baseReport} — likely: {fileName}";
    }

    private static string ExtractFileName(string url)
    {
        try
        {
            var path = new Uri(url, UriKind.Absolute).AbsolutePath;
            var segments = path.Split('/');
            var last = segments.LastOrDefault(s => !string.IsNullOrEmpty(s)) ?? url;
            // Strip query strings and hash fragments that survived
            var clean = last.Split('?')[0].Split('#')[0];
            return clean.Length > 40 ? clean[..40] + "..." : clean;
        }
        catch
        {
            return url.Length > 40 ? url[..40] + "..." : url;
        }
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024 => $"{bytes / 1_024.0:F1} KB",
        _ => $"{bytes} B"
    };
}
