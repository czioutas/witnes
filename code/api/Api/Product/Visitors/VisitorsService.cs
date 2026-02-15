using Api.Data;
using Api.Product.Visitors.Models;
using Libs.Pagination;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace Api.Product.Visitors;

/// <summary>
/// Service interface for retrieving visitor analytics data
/// </summary>
public interface IVisitorsService
{
    Task<PagedResult<VisitorSummaryModel>> GetVisitorsAsync(GetVisitorsRequest request);
    Task<PagedResult<PageLoadSummaryModel>> GetUserPageLoadsAsync(GetUserPageLoadsRequest request);
    Task<List<UserPageStatsResponse>> GetUserStatsAsync(string userOrGuestId);
}

/// <summary>
/// Service for retrieving visitor analytics data from Gold layer metrics
/// </summary>
public class VisitorsService : IVisitorsService
{
    private readonly ApplicationDbContextRead _contextRead;
    private readonly ILogger<VisitorsService> _logger;
    private readonly IMapper _mapper;


    public VisitorsService(
                IMapper mapper,
        ApplicationDbContextRead contextRead,
        ILogger<VisitorsService> logger)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _contextRead = contextRead ?? throw new ArgumentNullException(nameof(contextRead));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<PagedResult<VisitorSummaryModel>> GetVisitorsAsync(GetVisitorsRequest request)
    {
        _logger.LogInformation(
            "Retrieving visitors with filters: UserIdSearch={UserIdSearch}, StartDate={StartDate}, EndDate={EndDate}, Page={PageNumber}, PageSize={PageSize}",
            request.UserIdSearch,
            request.StartDate,
            request.EndDate,
            request.PageNumber,
            request.PageSize);

        // Build base query with filters
        var query = _contextRead.MetricsGold.AsNoTracking();

        // Apply user ID / guest ID search filter
        if (!string.IsNullOrWhiteSpace(request.UserIdSearch))
        {
            var search = request.UserIdSearch;
            query = query.Where(m =>
                (m.UserId != null && m.UserId.Contains(search)) ||
                (m.GuestId != null && m.GuestId.Contains(search)));
        }

        // Apply date range filters
        if (request.StartDate.HasValue)
        {
            var startDateUtc = request.StartDate.Value.Kind == DateTimeKind.Utc
                ? request.StartDate.Value
                : DateTime.SpecifyKind(request.StartDate.Value, DateTimeKind.Utc);
            query = query.Where(m => m.PageRequestedAtByVisitor >= startDateUtc);
        }

        if (request.EndDate.HasValue)
        {
            var endDateUtc = request.EndDate.Value.Kind == DateTimeKind.Utc
                ? request.EndDate.Value
                : DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc);
            query = query.Where(m => m.PageRequestedAtByVisitor <= endDateUtc);
        }

        // Group by UserId (if present) or GuestId (for anonymous visitors)
        var visitorQuery = query
            .GroupBy(m => new { UserId = m.UserId, GuestId = m.UserId != null ? null : m.GuestId })
            .Select(g => new VisitorSummaryModel
            {
                UserId = g.Key.UserId,
                GuestId = g.Key.GuestId,
                TotalPageLoads = g.Count(),
                LastSeenAt = g.Max(m => m.PageRequestedAtByVisitor),
                Browsers = g.Select(m => m.BrowserIcon).Distinct().ToList(),
                OperatingSystems = g.Select(m => m.DeviceIcon).Distinct().ToList()
            });

        // Get total count before pagination
        var totalCount = await visitorQuery.CountAsync();

        // Apply ordering (most recently seen first)
        var orderedQuery = visitorQuery.OrderByDescending(c => c.LastSeenAt);

        // Apply pagination
        var visitors = await orderedQuery
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync();

        _logger.LogInformation(
            "Retrieved {Count} visitors out of {TotalCount} total (Page {PageNumber})",
            visitors.Count,
            totalCount,
            request.PageNumber);

        return new PagedResult<VisitorSummaryModel>(
            visitors,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<PageLoadSummaryModel>> GetUserPageLoadsAsync(GetUserPageLoadsRequest request)
    {
        var isGuest = request.UserOrGuestId.StartsWith("w-gid_", StringComparison.Ordinal);

        _logger.LogInformation(
            "Retrieving page loads for {IdType}: Id={Id}, StartDate={StartDate}, EndDate={EndDate}, Page={PageNumber}, PageSize={PageSize}",
            isGuest ? "guest" : "user",
            request.UserOrGuestId,
            request.StartDate,
            request.EndDate,
            request.PageNumber,
            request.PageSize);

        // Build base query filtered by UserId or GuestId depending on prefix
        var query = _contextRead.MetricsGold.AsNoTracking();

        if (isGuest)
            query = query.Where(m => m.GuestId == request.UserOrGuestId && m.UserId == null);
        else
            query = query.Where(m => m.UserId == request.UserOrGuestId);

        // Apply date range filters
        if (request.StartDate.HasValue)
        {
            var startDateUtc = request.StartDate.Value.Kind == DateTimeKind.Utc
                ? request.StartDate.Value
                : DateTime.SpecifyKind(request.StartDate.Value, DateTimeKind.Utc);
            query = query.Where(m => m.PageRequestedAtByVisitor >= startDateUtc);
        }

        if (request.EndDate.HasValue)
        {
            var endDateUtc = request.EndDate.Value.Kind == DateTimeKind.Utc
                ? request.EndDate.Value
                : DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc);
            query = query.Where(m => m.PageRequestedAtByVisitor <= endDateUtc);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply ordering (most recent first) and pagination
        var pageLoads = await query
            .OrderByDescending(m => m.PageRequestedAtByVisitor)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync();

        var pageLoadsModel = _mapper.Map<List<PageLoadSummaryModel>>(pageLoads);

        _logger.LogInformation(
            "Retrieved {Count} page loads out of {TotalCount} total for {IdType} {Id} (Page {PageNumber})",
            pageLoads.Count,
            totalCount,
            isGuest ? "guest" : "user",
            request.UserOrGuestId,
            request.PageNumber);

        return new PagedResult<PageLoadSummaryModel>(
            pageLoadsModel,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }

    public async Task<List<UserPageStatsResponse>> GetUserStatsAsync(string userOrGuestId)
    {
        var stats = await _contextRead.UserPageStats
            .AsNoTracking()
            .Where(s => s.UserId == userOrGuestId)
            .Select(s => new UserPageStatsResponse
            {
                UserId = s.UserId,
                PagePath = s.PagePath,
                WindowType = s.WindowType,
                VisitCount = s.VisitCount,
                AvgLoadTimeMs = s.AvgLoadTimeMs,
                AvgDocTtfbMs = s.AvgDocTtfbMs,
                AvgApiTtfbWorstMs = s.AvgApiTtfbWorstMs,
                AvgTransferSizeBytes = s.AvgTransferSizeBytes,
                AvgCdnLoadTimeMs = s.AvgCdnLoadTimeMs
            })
            .ToListAsync();

        return stats;
    }
}
