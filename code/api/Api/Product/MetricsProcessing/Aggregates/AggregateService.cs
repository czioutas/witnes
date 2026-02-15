using Api.Data;
using Api.Product.MetricsProcessing.Gold;
using Microsoft.EntityFrameworkCore;

namespace Api.Product.MetricsProcessing.Aggregates;

public interface IAggregateService
{
    Task UpdateStatsForPageLoadAsync(MetricGoldEntity gold, Guid tenantId);
    Task RecalculateAllForTenantAsync(Guid tenantId);
}

public class AggregateService : IAggregateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AggregateService> _logger;

    public AggregateService(ApplicationDbContext context, ILogger<AggregateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task UpdateStatsForPageLoadAsync(MetricGoldEntity gold, Guid tenantId)
    {
        var userId = gold.UserId ?? gold.GuestId;
        if (userId == null) return;

        var pagePath = gold.UrlPath;

        // Fetch metrics from Silver for this page load
        var silver = await _context.MetricsSilver
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == gold.SilverId && s.TenantId == tenantId);

        if (silver == null) return;

        // --- Update count-based windows (last_10, last_50, last_200) ---
        // Bronze IS the ring buffer per spec — query from Silver (which has the computed values)
        await UpdateCountWindow(userId, pagePath, "last_10", 10, tenantId);
        await UpdateCountWindow(userId, pagePath, "last_50", 50, tenantId);
        await UpdateCountWindow(userId, pagePath, "last_200", 200, tenantId);

        // --- Update time-based windows (month, year) ---
        var month = gold.PageRequestedAtByVisitor.ToString("yyyy_MM");
        var year = gold.PageRequestedAtByVisitor.ToString("yyyy");

        await UpdateTimeWindow(userId, pagePath, $"month_{month}", silver, tenantId);
        await UpdateTimeWindow(userId, pagePath, $"year_{year}", silver, tenantId);

        await _context.SaveChangesAsync();
    }

    private async Task UpdateCountWindow(string userId, string pagePath, string windowType, int windowSize, Guid tenantId)
    {
        // Get the latest N silver records for this user + page
        var recentLoads = await _context.MetricsSilver
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId
                && (s.UserId == userId || s.GuestId == userId)
                && s.Url != null)
            .OrderByDescending(s => s.PageRequestedAtByVisitor)
            .Take(windowSize)
            .Select(s => new
            {
                s.AbsoluteLcpMs,
                s.InitialDocTtfbMs,
                s.SlowestApiTtfbMs,
                s.TotalPayloadBytes,
                s.CdnBaselineTtfbMs,
                PagePath = s.Url
            })
            .ToListAsync();

        // Filter to matching page path after extraction (URL → path)
        var filtered = recentLoads
            .Where(s => ExtractPath(s.PagePath) == pagePath)
            .ToList();

        if (!filtered.Any()) return;

        var stats = await GetOrCreateStats(userId, pagePath, windowType, tenantId);
        stats.VisitCount = filtered.Count;
        stats.AvgLoadTimeMs = filtered.Average(s => s.AbsoluteLcpMs);
        stats.AvgDocTtfbMs = filtered.Average(s => s.InitialDocTtfbMs);
        stats.AvgApiTtfbWorstMs = filtered.Average(s => s.SlowestApiTtfbMs);
        stats.AvgTransferSizeBytes = filtered.Average(s => s.TotalPayloadBytes);
        stats.AvgCdnLoadTimeMs = filtered.Average(s => s.CdnBaselineTtfbMs);
    }

    private async Task UpdateTimeWindow(string userId, string pagePath, string windowType,
        Silver.MetricSilverEntity silver, Guid tenantId)
    {
        var stats = await GetOrCreateStats(userId, pagePath, windowType, tenantId);

        // Increment sum + count
        stats.VisitCount++;
        stats.SumLoadTimeMs += silver.AbsoluteLcpMs;
        stats.SumDocTtfbMs += silver.InitialDocTtfbMs;
        stats.SumApiTtfbWorstMs += silver.SlowestApiTtfbMs;
        stats.SumTransferSizeBytes += silver.TotalPayloadBytes;
        stats.SumCdnLoadTimeMs += silver.CdnBaselineTtfbMs;

        // Recompute averages from sums
        stats.AvgLoadTimeMs = stats.SumLoadTimeMs / stats.VisitCount;
        stats.AvgDocTtfbMs = stats.SumDocTtfbMs / stats.VisitCount;
        stats.AvgApiTtfbWorstMs = stats.SumApiTtfbWorstMs / stats.VisitCount;
        stats.AvgTransferSizeBytes = stats.SumTransferSizeBytes / stats.VisitCount;
        stats.AvgCdnLoadTimeMs = stats.SumCdnLoadTimeMs / stats.VisitCount;
    }

    private async Task<UserPageStatsEntity> GetOrCreateStats(string userId, string pagePath, string windowType, Guid tenantId)
    {
        var existing = await _context.UserPageStats
            .FirstOrDefaultAsync(s =>
                s.TenantId == tenantId
                && s.UserId == userId
                && s.PagePath == pagePath
                && s.WindowType == windowType);

        if (existing != null) return existing;

        var entity = new UserPageStatsEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            PagePath = pagePath,
            WindowType = windowType
        };

        _context.UserPageStats.Add(entity);
        return entity;
    }

    public async Task RecalculateAllForTenantAsync(Guid tenantId)
    {
        _logger.LogInformation("Recalculating all aggregates for TenantId={TenantId}", tenantId);

        // Delete all existing stats
        await _context.UserPageStats
            .Where(s => s.TenantId == tenantId)
            .ExecuteDeleteAsync();

        // Re-process all Gold records in chronological order
        var goldRecords = await _context.MetricsGold
            .Where(g => g.TenantId == tenantId)
            .OrderBy(g => g.PageRequestedAtByVisitor)
            .ToListAsync();

        foreach (var gold in goldRecords)
        {
            await UpdateStatsForPageLoadAsync(gold, tenantId);
        }

        _logger.LogInformation("Aggregate recalculation complete for TenantId={TenantId}. Processed {Count} records.", tenantId, goldRecords.Count);
    }

    private static string ExtractPath(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var path = uri.AbsolutePath;
            return path == "/" ? "/" : path.TrimEnd('/');
        }
        return url.Split('?')[0];
    }
}
