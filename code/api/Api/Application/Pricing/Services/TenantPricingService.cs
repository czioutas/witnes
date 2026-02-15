using System.Text.Json;
using Api.Application.Pricing.Entities;
using Api.Application.Pricing.Models;
using Api.Application.Services;
using Api.Application.Tenancy.Services;
using Api.Data;
using AutoMapper;
using Libs.Result;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Api.Application.Pricing.Services;

public interface ITenantPricingService
{
    Task<Result<TenantPricingTierEntity>> SetTenantPricingAsync(Guid pricingTierId, DateOnly? startDate = null, bool isTrial = false);
    Task<Result<TenantPricingTierEntity>> ChangeTenantPricingAsync(Guid newPricingTierId, DateOnly? startDate = null);
    Task<Result<TenantPricingTierEntity>> GetCurrentTenantPricingAsync();
    Task<Result<TenantPricingTierEntity>> GetTenantPricingAtDateAsync(DateOnly date);
    Task<Result<List<TenantPricingTierEntity>>> GetTenantPricingHistoryAsync();
    Task<Result<bool>> EndTenantPricingAsync(DateOnly? endDate = null);
    Task<PricingTierEntity?> GetStarterTierAsync();
    Task<Result<List<TenantPricingTierEntity>>> GetTenantPricingForPeriodAsync(DateOnly periodStart, DateOnly periodEnd);
    Task<Result<bool>> SetTrialExpiredAsync(Guid tenantPricingId);
}

public class TenantPricingService : ITenantPricingService
{
    private readonly IRequestTenant _requestTenant;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TenantPricingService> _logger;
    private readonly IDatabase _redis;
    private readonly IMapper _mapper;

    public TenantPricingService(IRequestTenant requestTenant, ApplicationDbContext context, ILogger<TenantPricingService> logger, IConnectionMultiplexer redisConnection, IMapper mapper)
    {
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _redis = redisConnection.GetDatabase() ?? throw new ArgumentNullException(nameof(redisConnection));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public static string GetTenantPricingCacheKey(Guid tenantId)
    {
        return CacheKeyGeneratorService.GenerateCacheKey<TenantPricingTierEntity>("pricing", tenantId.ToString()).Key;
    }

    private async Task CacheTenantPricingAsync(Guid tenantId, TenantPricingTierEntity tenantPricing)
    {
        var cacheKey = GetTenantPricingCacheKey(tenantId);
        var cacheValue = _mapper.Map<TenantPricingCacheModel>(tenantPricing);

        await _redis.StringSetAsync(cacheKey, JsonSerializer.Serialize(cacheValue), TimeSpan.FromHours(24));
    }

    public async Task<Result<TenantPricingTierEntity>> SetTenantPricingAsync(Guid pricingTierId, DateOnly? startDate = null, bool isTrial = false)
    {
        try
        {
            var effectiveStartDate = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            // Check if pricing tier exists
            var pricingTier = await _context.PricingTiers
                .FirstOrDefaultAsync(pt => pt.Id == pricingTierId && pt.IsActive);

            if (pricingTier == null)
            {
                return Result<TenantPricingTierEntity>.NotFound("PricingTier", pricingTierId.ToString());
            }

            // Check if tenant already has pricing for this date
            var existingPricing = await _context.TenantPricingTiers
                .Where(tp => tp.StartDate <= effectiveStartDate && (tp.EndDate == null || tp.EndDate >= effectiveStartDate))
                .FirstOrDefaultAsync();

            if (existingPricing != null)
            {
                return Result<TenantPricingTierEntity>.BusinessError("TenantPricingOverlap", "Tenant already has pricing for this date");
            }

            var tenantPricing = new TenantPricingTierEntity(pricingTierId, effectiveStartDate, hasTrial: isTrial)
            {
                TenantId = _requestTenant.TenantId
            };

            _context.TenantPricingTiers.Add(tenantPricing);
            await _context.SaveChangesAsync();

            // Load the pricing tier for caching
            await _context.Entry(tenantPricing)
                .Reference(tp => tp.PricingTier)
                .LoadAsync();

            // Cache the pricing information
            await CacheTenantPricingAsync(_requestTenant.TenantId, tenantPricing);

            return Result<TenantPricingTierEntity>.Ok(tenantPricing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting tenant pricing for tenant {TenantId}", _requestTenant.TenantId);
            return Result<TenantPricingTierEntity>.Failure(new InternalErrorModel(ex));
        }
    }

    public async Task<Result<TenantPricingTierEntity>> ChangeTenantPricingAsync(Guid newPricingTierId, DateOnly? startDate = null)
    {
        try
        {
            var effectiveStartDate = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            // Check if new pricing tier exists
            var newPricingTier = await _context.PricingTiers
                .FirstOrDefaultAsync(pt => pt.Id == newPricingTierId && pt.IsActive);

            if (newPricingTier == null)
            {
                return Result<TenantPricingTierEntity>.NotFound("PricingTier", newPricingTierId.ToString());
            }

            // End current pricing tier
            var currentPricingResult = await GetCurrentTenantPricingAsync();
            if (currentPricingResult.IsFailure && currentPricingResult.ErrorModel is not ResourceNotFoundErrorModel)
            {
                return currentPricingResult.ToErrorResult<TenantPricingTierEntity>();
            }

            if (!currentPricingResult.IsFailure)
            {
                var currentPricing = currentPricingResult.GetValue;
                currentPricing.EndDate = effectiveStartDate.AddDays(-1);
                currentPricing.IsActive = false;
                _context.TenantPricingTiers.Update(currentPricing);
            }

            // Create new pricing tier
            var newTenantPricing = new TenantPricingTierEntity(newPricingTierId, effectiveStartDate)
            {
                TenantId = _requestTenant.TenantId
            };

            _context.TenantPricingTiers.Add(newTenantPricing);
            await _context.SaveChangesAsync();

            // Load the pricing tier for caching
            await _context.Entry(newTenantPricing)
                .Reference(tp => tp.PricingTier)
                .LoadAsync();

            // Cache the pricing information
            await CacheTenantPricingAsync(_requestTenant.TenantId, newTenantPricing);

            return Result<TenantPricingTierEntity>.Ok(newTenantPricing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing tenant pricing for tenant {TenantId}", _requestTenant.TenantId);
            return Result<TenantPricingTierEntity>.Failure(new InternalErrorModel(ex));
        }
    }

    public async Task<Result<TenantPricingTierEntity>> GetCurrentTenantPricingAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var pricing = await _context.TenantPricingTiers
            .Include(tp => tp.PricingTier)
            .Where(tp =>
                        tp.StartDate <= today &&
                        (tp.EndDate == null || tp.EndDate >= today) &&
                        tp.IsActive)
            .FirstOrDefaultAsync();

        if (pricing == null)
        {
            return Result<TenantPricingTierEntity>.NotFound("TenantPricing", _requestTenant.TenantId.ToString());
        }

        return Result<TenantPricingTierEntity>.Ok(pricing);
    }

    public async Task<Result<TenantPricingTierEntity>> GetTenantPricingAtDateAsync(DateOnly date)
    {
        var pricing = await _context.TenantPricingTiers
            .Include(tp => tp.PricingTier)
            .Where(tp =>
                        tp.StartDate <= date &&
                        (tp.EndDate == null || tp.EndDate >= date) &&
                        tp.IsActive)
            .FirstOrDefaultAsync();

        if (pricing == null)
        {
            return Result<TenantPricingTierEntity>.NotFound("TenantPricingAtDate", date.ToString("yyyy-MM-dd"));
        }

        return Result<TenantPricingTierEntity>.Ok(pricing);
    }

    public async Task<Result<List<TenantPricingTierEntity>>> GetTenantPricingHistoryAsync()
    {
        var history = await _context.TenantPricingTiers
            .Include(tp => tp.PricingTier)
            .OrderBy(tp => tp.StartDate)
            .ToListAsync();

        return Result<List<TenantPricingTierEntity>>.Ok(history);
    }

    public async Task<Result<bool>> EndTenantPricingAsync(DateOnly? endDate = null)
    {
        try
        {
            var effectiveEndDate = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var currentPricingResult = await GetCurrentTenantPricingAsync();
            if (currentPricingResult.IsFailure)
            {
                return currentPricingResult.ToErrorResult<bool>();
            }
            var currentPricing = currentPricingResult.GetValue;

            currentPricing.EndDate = effectiveEndDate;
            currentPricing.IsActive = false;

            _context.TenantPricingTiers.Update(currentPricing);
            await _context.SaveChangesAsync();

            // Clear the cache since pricing is no longer active
            var cacheKey = GetTenantPricingCacheKey(_requestTenant.TenantId);
            await _redis.KeyDeleteAsync(cacheKey);

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending tenant pricing for tenant {TenantId}", _requestTenant.TenantId);
            return Result<bool>.Failure(new InternalErrorModel(ex));
        }
    }

    public async Task<PricingTierEntity?> GetStarterTierAsync()
    {
        return await _context.PricingTiers
            .Where(pt => pt.Name.ToLower() == "starter" && pt.IsActive)
            .FirstOrDefaultAsync();
    }

    public async Task<Result<List<TenantPricingTierEntity>>> GetTenantPricingForPeriodAsync(DateOnly periodStart, DateOnly periodEnd)
    {
        var pricing = await _context.TenantPricingTiers
            .Include(tp => tp.PricingTier)
            .Where(tp =>
                        tp.StartDate <= periodEnd &&
                        (tp.EndDate == null || tp.EndDate >= periodStart))
            .OrderBy(tp => tp.StartDate)
            .ToListAsync();

        return Result<List<TenantPricingTierEntity>>.Ok(pricing);
    }

    public async Task<Result<bool>> SetTrialExpiredAsync(Guid tenantPricingId)
    {
        try
        {
            var pricing = await _context.TenantPricingTiers
                .FirstOrDefaultAsync(tp => tp.Id == tenantPricingId);

            if (pricing == null)
            {
                return Result<bool>.NotFound("TenantPricing", tenantPricingId.ToString());
            }

            pricing.HasTrialExpired = true;
            _context.TenantPricingTiers.Update(pricing);
            await _context.SaveChangesAsync();

            // Update cache
            await _context.Entry(pricing)
                .Reference(tp => tp.PricingTier)
                .LoadAsync();
            await CacheTenantPricingAsync(pricing.TenantId, pricing);

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting trial expired for tenant pricing {TenantPricingId}", tenantPricingId);
            return Result<bool>.Failure(new InternalErrorModel(ex));
        }
    }
}
