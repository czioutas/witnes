using Api.Data;
using Api.Product.Visitors.Models;
using Libs.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Api.Product.Visitors;

/// <summary>
/// Service interface for retrieving visitor analytics data
/// </summary>
public interface IVisitorsService
{
    /// <summary>
    /// Gets a paginated list of visitors with their activity summary
    /// </summary>
    /// <param name="request">Filter and pagination parameters</param>
    /// <returns>Paginated result containing visitor summaries</returns>
    Task<PagedResult<VisitorSummaryModel>> GetVisitorsAsync(GetVisitorsRequest request);

    /// <summary>
    /// Gets a paginated list of page loads for a specific user
    /// </summary>
    /// <param name="request">User ID and filter parameters</param>
    /// <returns>Paginated result containing page load summaries</returns>
    Task<PagedResult<PageLoadSummaryModel>> GetUserPageLoadsAsync(GetUserPageLoadsRequest request);
}

/// <summary>
/// Service for retrieving visitor analytics data from Gold layer metrics
/// </summary>
public class VisitorsService : IVisitorsService
{
    private readonly ApplicationDbContextRead _contextRead;
    private readonly ILogger<VisitorsService> _logger;

    public VisitorsService(
        ApplicationDbContextRead contextRead,
        ILogger<VisitorsService> logger)
    {
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
            query = query.Where(m => m.Timestamp >= startDateUtc);
        }

        if (request.EndDate.HasValue)
        {
            var endDateUtc = request.EndDate.Value.Kind == DateTimeKind.Utc
                ? request.EndDate.Value
                : DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc);
            query = query.Where(m => m.Timestamp <= endDateUtc);
        }

        // Group by UserId (if present) or GuestId (for anonymous visitors)
        var visitorQuery = query
            .GroupBy(m => new { UserId = m.UserId, GuestId = m.UserId != null ? null : m.GuestId })
            .Select(g => new VisitorSummaryModel
            {
                UserId = g.Key.UserId,
                GuestId = g.Key.GuestId,
                TotalPageLoads = g.Count(),
                LastSeenAt = g.Max(m => m.Timestamp),
                Browsers = new List<string>(),
                OperatingSystems = new List<string>()
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
            query = query.Where(m => m.Timestamp >= startDateUtc);
        }

        if (request.EndDate.HasValue)
        {
            var endDateUtc = request.EndDate.Value.Kind == DateTimeKind.Utc
                ? request.EndDate.Value
                : DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc);
            query = query.Where(m => m.Timestamp <= endDateUtc);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply ordering (most recent first) and pagination
        var pageLoads = await query
            .OrderByDescending(m => m.Timestamp)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(m => new PageLoadSummaryModel
            {
                Id = m.Id,
                SessionId = m.SessionId,
                Timestamp = m.Timestamp,
                UrlPath = m.UrlPath,
                LcpMs = m.LcpMs,
                LcpVerdict = m.LcpVerdict,
                ClsScore = m.ClsScore,
                ClsVerdict = m.ClsVerdict,
                IsConnectionFault = m.IsConnectionFault,
                ConnectionQuality = m.ConnectionQuality,
                ConnectionReasons = m.ConnectionReasons,
                EffectiveType = m.EffectiveType,
                Rtt = m.Rtt,
                Downlink = m.Downlink,
                TtfbMs = m.TtfbMs,
                IsBackendFault = m.IsBackendFault,
                IsFrontendFault = m.IsFrontendFault,
                DeviceIcon = m.DeviceIcon,
                BrowserIcon = m.BrowserIcon,
                Incomplete = m.Incomplete,
                SilverId = m.SilverId
            })
            .ToListAsync();

        _logger.LogInformation(
            "Retrieved {Count} page loads out of {TotalCount} total for {IdType} {Id} (Page {PageNumber})",
            pageLoads.Count,
            totalCount,
            isGuest ? "guest" : "user",
            request.UserOrGuestId,
            request.PageNumber);

        return new PagedResult<PageLoadSummaryModel>(
            pageLoads,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
