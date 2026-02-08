using Api.Application.Tenancy.Services;
using Api.Data;
using Libs.Domain;
using Libs.Result;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Limits.Services;

public interface ILimitsService
{
    /// <summary>
    /// Checks if the tenant can increment usage without exceeding the limit.
    /// </summary>
    /// <param name="limitKey">The resource type to check</param>
    /// <param name="amount">Amount to increment (default: 1)</param>
    /// <returns>True if increment is allowed, false if it would exceed the limit</returns>
    Task<bool> CanIncrementAsync(LimitKey limitKey, int amount = 1);

    /// <summary>
    /// Increments the usage counter for a resource. Returns error if limit would be exceeded.
    /// </summary>
    /// <param name="limitKey">The resource type to increment</param>
    /// <param name="amount">Amount to increment (default: 1)</param>
    /// <returns>Result indicating success or limit exceeded error</returns>
    Task<Result<bool>> IncrementAsync(LimitKey limitKey, int amount = 1);

    /// <summary>
    /// Decrements the usage counter for a resource.
    /// </summary>
    /// <param name="limitKey">The resource type to decrement</param>
    /// <param name="amount">Amount to decrement (default: 1)</param>
    /// <returns>Result indicating success</returns>
    Task<Result<bool>> DecrementAsync(LimitKey limitKey, int amount = 1);

    /// <summary>
    /// Checks if the tenant is currently over their limit.
    /// </summary>
    /// <param name="limitKey">The resource type to check</param>
    /// <returns>True if over limit, false otherwise</returns>
    Task<bool> IsOverLimitAsync(LimitKey limitKey);
}

/// <summary>
/// Service for managing and enforcing tenant resource limits.
/// Tracks usage and enforces limits for various resources (locations, etc.) per tenant.
/// </summary>
public class LimitsService : ILimitsService
{
    private readonly ApplicationDbContext _context;
    private readonly IRequestTenant _requestTenant;
    private readonly ILogger<LimitsService> _logger;

    public LimitsService(
        ApplicationDbContext context,
        IRequestTenant requestTenant,
        ILogger<LimitsService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> CanIncrementAsync(LimitKey limitKey, int amount = 1)
    {
        var tenantId = _requestTenant.TenantId;

        var limit = await _context.TenantLimits
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.LimitKey == limitKey);

        // No limit configured - allow operation but log warning
        if (limit == null)
        {
            _logger.LogWarning(
                "No limit configured for tenant {TenantId} and limit key {LimitKey}. Operation allowed but should configure limits.",
                tenantId, limitKey);
            return true;
        }

        // Check if increment would exceed limit
        return limit.CurrentAmount + amount <= limit.MaxAmount;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> IncrementAsync(LimitKey limitKey, int amount = 1)
    {
        var tenantId = _requestTenant.TenantId;

        var limit = await _context.TenantLimits
            .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.LimitKey == limitKey);

        // No limit configured - allow operation but log warning
        if (limit == null)
        {
            _logger.LogWarning(
                "No limit configured for tenant {TenantId} and limit key {LimitKey}. Operation allowed but should configure limits.",
                tenantId, limitKey);
            return Result<bool>.Ok(true);
        }

        // Check if increment would exceed limit
        if (limit.CurrentAmount + amount > limit.MaxAmount)
        {
            return Result<bool>.BusinessError(
                "LimitExceeded",
                $"Cannot create more {limitKey.ToString().ToLower()}. You have reached your limit of {limit.MaxAmount} {limitKey.ToString().ToLower()} per {limit.Period.ToString().ToLower()}.");
        }

        // Increment the counter
        limit.CurrentAmount += amount;
        await _context.SaveChangesAsync();

        return Result<bool>.Ok(true);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DecrementAsync(LimitKey limitKey, int amount = 1)
    {
        var tenantId = _requestTenant.TenantId;

        var limit = await _context.TenantLimits
            .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.LimitKey == limitKey);

        // No limit configured - nothing to decrement
        if (limit == null)
        {
            _logger.LogWarning(
                "No limit configured for tenant {TenantId} and limit key {LimitKey}. Decrement operation skipped.",
                tenantId, limitKey);
            return Result<bool>.Ok(true);
        }

        // Decrement the counter (don't go below 0)
        limit.CurrentAmount = Math.Max(0, limit.CurrentAmount - amount);
        await _context.SaveChangesAsync();

        return Result<bool>.Ok(true);
    }

    /// <inheritdoc />
    public async Task<bool> IsOverLimitAsync(LimitKey limitKey)
    {
        var tenantId = _requestTenant.TenantId;

        var limit = await _context.TenantLimits
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.LimitKey == limitKey);

        // No limit configured - not over limit
        if (limit == null)
        {
            return false;
        }

        return limit.CurrentAmount > limit.MaxAmount;
    }
}
