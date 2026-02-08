using Api.Application.Services;
using Api.Application.Tenancy.Services;
using Api.Data;
using Libs.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using ZiggyCreatures.Caching.Fusion;

namespace Api.Application.Features.Services;

/// <summary>
/// Database-backed feature definition provider for Microsoft.FeatureManagement.
/// This provider is registered as a singleton but accesses scoped services (IRequestTenant, ApplicationDbContext)
/// from the current HTTP request's service scope using IHttpContextAccessor.
/// Uses IgnoreQueryFilters() to explicitly filter tenant-specific features by tenant ID.
/// Features are cached for 15 minutes using FusionCache to improve performance.
/// </summary>
public class DatabaseFeatureDefinitionProvider : IFeatureDefinitionProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IFusionCache _cache;
    private readonly TimeProvider _timeProvider;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    public DatabaseFeatureDefinitionProvider(
        IHttpContextAccessor httpContextAccessor,
        IFusionCache cache,
        TimeProvider timeProvider)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
    {
        // Get scoped services from the current HTTP request's service provider
        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available");
        var dbContext = httpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var requestTenant = httpContext.RequestServices.GetRequiredService<IRequestTenant>();

        // Get tenant ID from the HTTP request's scoped IRequestTenant (already set by middleware)
        var tenantId = requestTenant.TenantId;
        var cacheKey = CacheKeyGeneratorService.GenerateCacheKey<FeatureDefinition>("tenant", tenantId.ToString()).Key;

        var enabledFeatureKeys = await _cache.GetOrSetAsync(
            cacheKey,
            async _ => await GetEnabledFeatureKeysFromDbAsync(dbContext, tenantId),
            new FusionCacheEntryOptions { Duration = CacheDuration });

        foreach (var featureKey in enabledFeatureKeys)
        {
            yield return new FeatureDefinition
            {
                Name = featureKey.ToString(),
                EnabledFor = new[] { new FeatureFilterConfiguration { Name = "AlwaysOn" } }
            };
        }
    }

    public async Task<FeatureDefinition?> GetFeatureDefinitionAsync(string featureName)
    {
        // Try to parse the feature name to enum
        if (!Enum.TryParse<FeatureKey>(featureName, true, out var featureKey))
        {
            return null;
        }

        // Get scoped services from the current HTTP request's service provider
        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available");
        var dbContext = httpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var requestTenant = httpContext.RequestServices.GetRequiredService<IRequestTenant>();

        // Get tenant ID from the HTTP request's scoped IRequestTenant (already set by middleware)
        var tenantId = requestTenant.TenantId;
        var cacheKey = CacheKeyGeneratorService.GenerateCacheKey<FeatureDefinition>("feature", featureName, "tenant", tenantId.ToString()).Key;

        var isEnabled = await _cache.GetOrSetAsync(
            cacheKey,
            async _ => await IsFeatureEnabledForTenantAsync(dbContext, featureKey, tenantId),
            new FusionCacheEntryOptions { Duration = CacheDuration });

        if (!isEnabled)
        {
            return null;
        }

        return new FeatureDefinition
        {
            Name = featureKey.ToString(),
            EnabledFor = new[] { new FeatureFilterConfiguration { Name = "AlwaysOn" } }
        };
    }

    private async Task<List<FeatureKey>> GetEnabledFeatureKeysFromDbAsync(ApplicationDbContext dbContext, Guid tenantId)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().DateTime);

        // Get all global features that are enabled by default
        var globalFeatures = await dbContext.Features
            .AsNoTracking()
            .Where(f => f.IsGlobal && f.IsEnabledByDefault)
            .Select(f => f.Key)
            .ToListAsync();

        // Get tenant-specific enabled features - ignore query filters since this is a singleton
        var tenantFeatures = await dbContext.TenantFeatures
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(ft => ft.Feature)
            .Where(ft =>
                ft.TenantId == tenantId
                && ft.IsEnabled
                && (ft.EnabledFrom == null || ft.EnabledFrom <= today)
                && (ft.EnabledUntil == null || ft.EnabledUntil >= today)
                && !ft.Feature.IsGlobal)
            .Select(ft => ft.Feature.Key)
            .ToListAsync();

        return globalFeatures.Union(tenantFeatures).ToList();
    }

    private async Task<bool> IsFeatureEnabledForTenantAsync(ApplicationDbContext dbContext, FeatureKey featureKey, Guid tenantId)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().DateTime);

        // Check if feature exists and get its configuration
        var feature = await dbContext.Features
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Key == featureKey);

        if (feature == null)
        {
            return false;
        }

        // If feature is global and enabled by default, return true
        if (feature.IsGlobal)
        {
            return feature.IsEnabledByDefault;
        }

        // Check tenant-specific feature toggle - ignore query filters since this is a singleton
        var featureTenant = await dbContext.TenantFeatures
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(ft => ft.FeatureId == feature.Id && ft.TenantId == tenantId);

        if (featureTenant == null)
        {
            return false;
        }

        // Check if feature is enabled and within date range
        return featureTenant.IsEnabled
            && (featureTenant.EnabledFrom == null || featureTenant.EnabledFrom <= today)
            && (featureTenant.EnabledUntil == null || featureTenant.EnabledUntil >= today);
    }
}
