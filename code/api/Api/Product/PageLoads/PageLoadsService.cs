using Api.Data;
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
            UserId = silverMetric.UserId ?? silverMetric.GuestId ?? "Unknown",
            Url = silverMetric.Url,
            Timestamp = silverMetric.PageRequestedAtByVisitor,
            LcpMs = silverMetric.AbsoluteLcpMs,
            Cls = silverMetric.CumulativeLayoutShift,
            AvgTtfbMs = silverMetric.InitialDocTtfbMs,
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
                .Select(j => $"Long task at {j.S}ms (duration: {j.D}ms)")
                .ToList()
        };

        _logger.LogInformation(
            "Retrieved page load detail: {PageLoadId}, URL: {Url}, Resources: {ResourceCount}, Jank Reports: {JankCount}",
            id,
            detail.Url,
            detail.Waterfall.Count,
            detail.JankReports.Count);

        return detail;
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024 => $"{bytes / 1_024.0:F1} KB",
        _ => $"{bytes} B"
    };
}
