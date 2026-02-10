using Api.Application.Services;
using Api.Application.Settings;
using Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace Api.Product.Cors.Services;

public interface IAllowedOriginService
{
    Task<HashSet<string>> GetAllowedOriginsAsync();
    Task InvalidateCacheAsync();
}

public class AllowedOriginService : IAllowedOriginService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFusionCache _cache;
    private readonly CorsSettings _corsSettings;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private static readonly string CacheKey =
        CacheKeyGeneratorService.GenerateCacheKey<AllowedOriginService>("allowed-origins").Key;

    public AllowedOriginService(
        IServiceScopeFactory scopeFactory,
        IFusionCache cache,
        IOptions<CorsSettings> corsSettings)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _corsSettings = corsSettings.Value;
    }

    public async Task<HashSet<string>> GetAllowedOriginsAsync()
    {
        var origins = await _cache.GetOrSetAsync<HashSet<string>>(
            CacheKey,
            async (ctx, ct) => await LoadOriginsAsync(),
            new FusionCacheEntryOptions { Duration = CacheDuration }
        );

        return origins ?? [];
    }

    public async Task InvalidateCacheAsync()
    {
        await _cache.RemoveAsync(CacheKey);
    }

    private async Task<HashSet<string>> LoadOriginsAsync()
    {
        var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add static origins from appsettings
        if (!string.IsNullOrWhiteSpace(_corsSettings.Origins))
        {
            foreach (var origin in _corsSettings.Origins.Split(",", StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = origin.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    origins.Add(trimmed);
                }
            }
        }

        // Add dynamic origins from project keys
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContextRead>();

        var domains = await dbContext.ProjectKeys
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(pk => pk.IsActive && pk.Domain != "")
            .Select(pk => pk.Domain)
            .Distinct()
            .ToListAsync();

        foreach (var domain in domains)
        {
            origins.Add($"https://{domain}");
        }

        return origins;
    }
}
