using Api.Data;
using Api.Product.TenantCustomers.Models;
using Libs.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Api.Product.TenantCustomers;

/// <summary>
/// Service interface for retrieving tenant customer analytics data
/// </summary>
public interface ITenantCustomersService
{
    /// <summary>
    /// Gets a paginated list of tenant customers with their activity summary
    /// </summary>
    /// <param name="request">Filter and pagination parameters</param>
    /// <returns>Paginated result containing tenant customer summaries</returns>
    Task<PagedResult<TenantCustomerSummaryModel>> GetTenantCustomersAsync(GetTenantCustomersRequest request);

    /// <summary>
    /// Gets a paginated list of page loads for a specific user
    /// </summary>
    /// <param name="request">User ID and filter parameters</param>
    /// <returns>Paginated result containing page load summaries</returns>
    Task<PagedResult<PageLoadSummaryModel>> GetUserPageLoadsAsync(GetUserPageLoadsRequest request);
}

/// <summary>
/// Service for retrieving tenant customer analytics data from Gold layer metrics
/// </summary>
public class TenantCustomersService : ITenantCustomersService
{
    private readonly ApplicationDbContextRead _contextRead;
    private readonly ILogger<TenantCustomersService> _logger;

    public TenantCustomersService(
        ApplicationDbContextRead contextRead,
        ILogger<TenantCustomersService> logger)
    {
        _contextRead = contextRead ?? throw new ArgumentNullException(nameof(contextRead));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<PagedResult<TenantCustomerSummaryModel>> GetTenantCustomersAsync(GetTenantCustomersRequest request)
    {
        _logger.LogInformation(
            "Retrieving tenant customers with filters: UserIdSearch={UserIdSearch}, StartDate={StartDate}, EndDate={EndDate}, Page={PageNumber}, PageSize={PageSize}",
            request.UserIdSearch,
            request.StartDate,
            request.EndDate,
            request.PageNumber,
            request.PageSize);

        // Build base query with filters
        var query = _contextRead.MetricsGold.AsNoTracking();

        // Apply user ID search filter
        if (!string.IsNullOrWhiteSpace(request.UserIdSearch))
        {
            query = query.Where(m => m.UserId.Contains(request.UserIdSearch));
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

        // Group by UserId and aggregate metrics
        var customerQuery = query
            .GroupBy(m => m.UserId)
            .Select(g => new TenantCustomerSummaryModel
            {
                UserId = g.Key,
                TotalPageLoads = g.Count(),
                LastSeenAt = g.Max(m => m.Timestamp),
                // Browsers and OperatingSystems are empty for MVP
                // These will be populated in future enhancement when we add device info to MetricsGold
                Browsers = new List<string>(),
                OperatingSystems = new List<string>()
            });

        // Get total count before pagination
        var totalCount = await customerQuery.CountAsync();

        // Apply ordering (most recently seen first)
        var orderedQuery = customerQuery.OrderByDescending(c => c.LastSeenAt);

        // Apply pagination
        var customers = await orderedQuery
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync();

        _logger.LogInformation(
            "Retrieved {Count} tenant customers out of {TotalCount} total (Page {PageNumber})",
            customers.Count,
            totalCount,
            request.PageNumber);

        return new PagedResult<TenantCustomerSummaryModel>(
            customers,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<PageLoadSummaryModel>> GetUserPageLoadsAsync(GetUserPageLoadsRequest request)
    {
        _logger.LogInformation(
            "Retrieving page loads for user: UserId={UserId}, StartDate={StartDate}, EndDate={EndDate}, Page={PageNumber}, PageSize={PageSize}",
            request.UserId,
            request.StartDate,
            request.EndDate,
            request.PageNumber,
            request.PageSize);

        // Build base query filtered by UserId
        var query = _contextRead.MetricsGold
            .AsNoTracking()
            .Where(m => m.UserId == request.UserId);

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
            "Retrieved {Count} page loads out of {TotalCount} total for user {UserId} (Page {PageNumber})",
            pageLoads.Count,
            totalCount,
            request.UserId,
            request.PageNumber);

        return new PagedResult<PageLoadSummaryModel>(
            pageLoads,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
