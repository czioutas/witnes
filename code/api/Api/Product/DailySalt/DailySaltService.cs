using System.Security.Cryptography;
using Api.Application.Services;
using Api.Data;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace Api.Product.DailySalt;

public interface IDailySaltService
{
    /// <summary>
    /// Gets today's salt, creating a new one if the current salt has expired.
    /// Cached with absolute expiry at midnight UTC.
    /// </summary>
    Task<string> GetTodaySaltAsync();
}

public class DailySaltService : IDailySaltService
{
    private readonly IFusionCache _cache;
    private readonly ILogger<DailySaltService> _logger;
    private readonly string _cacheKey = "daily-salt-active";
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly TimeProvider _dateTimeProvider;

    public DailySaltService(
        IFusionCache cache,
        TimeProvider dateTimeProvider,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<DailySaltService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheKey = CacheKeyGeneratorService.GenerateCacheKey<DailySaltEntity>("active").Key;
    }

    public async Task<string> GetTodaySaltAsync()
    {
        var now = _dateTimeProvider.GetUtcNow();
        var midnight = now.Date.AddDays(1);
        var duration = midnight - now;

        return await _cache.GetOrSetAsync(
            _cacheKey,
            async _ => await LoadOrCreateSaltAsync(),
            new FusionCacheEntryOptions { Duration = duration }
        );
    }

    private async Task<string> LoadOrCreateSaltAsync()
    {
        var today = DateOnly.FromDateTime(_dateTimeProvider.GetUtcNow().DateTime);

        using var scope = _serviceScopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existing = await _context.Set<DailySaltEntity>().FirstOrDefaultAsync();

        if (existing != null && existing.ActiveDate == today)
        {
            _logger.LogDebug("Using existing daily salt for {Date}", today);
            return existing.Salt;
        }

        var newSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        if (existing != null)
        {
            existing.Salt = newSalt;
            existing.ActiveDate = today;
            _logger.LogInformation("Rotated daily salt for {Date}", today);
        }
        else
        {
            _context.Set<DailySaltEntity>().Add(new DailySaltEntity
            {
                Salt = newSalt,
                ActiveDate = today
            });
            _logger.LogInformation("Created initial daily salt for {Date}", today);
        }

        await _context.SaveChangesAsync();
        return newSalt;
    }
}
