using Api.Application.Pricing.Services;
using Api.Application.Tenancy.Services;
using Api.Product.MetricsProcessing.Bronze;
using Api.Product.MetricsProcessing.Gold;
using Api.Product.MetricsProcessing.Silver;

namespace Api.Product.MetricCleanup;

public interface IMetricCleanupService
{
    Task RunCleanupAsync();
}

public class MetricCleanupService : IMetricCleanupService
{
    private readonly ITenantService _tenantService;
    private readonly ITenantPricingService _tenantPricingService;
    private readonly IBronzeService _bronzeService;
    private readonly ISilverService _silverService;
    private readonly IGoldService _goldService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MetricCleanupService> _logger;

    public MetricCleanupService(
        ITenantService tenantService,
        ITenantPricingService tenantPricingService,
        IBronzeService bronzeService,
        ISilverService silverService,
        IGoldService goldService,
        TimeProvider timeProvider,
        ILogger<MetricCleanupService> logger)
    {
        _tenantService = tenantService;
        _tenantPricingService = tenantPricingService;
        _bronzeService = bronzeService;
        _silverService = silverService;
        _goldService = goldService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task RunCleanupAsync()
    {
        _logger.LogInformation("[MetricCleanup] Starting metric data cleanup");

        var tenantIds = await _tenantService.GetAllTenantIdsAsync();
        _logger.LogInformation("[MetricCleanup] Found {Count} tenants to process", tenantIds.Count);

        var totalDeleted = 0;

        foreach (var tenantId in tenantIds)
        {
            try
            {
                var deleted = await CleanupTenantAsync(tenantId);
                totalDeleted += deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MetricCleanup] Error cleaning up tenant {TenantId}", tenantId);
            }
        }

        _logger.LogInformation("[MetricCleanup] Cleanup complete. Total records deleted: {TotalDeleted}", totalDeleted);
    }

    private async Task<int> CleanupTenantAsync(Guid tenantId)
    {
        var pricing = await _tenantPricingService.GetCurrentTenantPricingIgnoreFiltersAsync(tenantId);

        if (pricing?.PricingTier == null)
        {
            _logger.LogWarning("[MetricCleanup] Tenant {TenantId} has no active pricing tier, skipping", tenantId);
            return 0;
        }

        var retentionDays = pricing.PricingTier.DataRetentionDays;
        if (retentionDays <= 0)
        {
            _logger.LogDebug("[MetricCleanup] Tenant {TenantId} has unlimited retention, skipping", tenantId);
            return 0;
        }

        var cutoff = _timeProvider.GetUtcNow().AddDays(-retentionDays);

        _logger.LogInformation(
            "[MetricCleanup] Tenant {TenantId}: retention={RetentionDays}d, cutoff={CutoffDate}",
            tenantId, retentionDays, cutoff);

        var bronzeDeleted = await _bronzeService.DeleteOlderThanAsync(tenantId, cutoff);
        var silverDeleted = await _silverService.DeleteOlderThanAsync(tenantId, cutoff);
        var goldDeleted = await _goldService.DeleteOlderThanAsync(tenantId, cutoff);

        var total = bronzeDeleted + silverDeleted + goldDeleted;

        if (total > 0)
        {
            _logger.LogInformation(
                "[MetricCleanup] Tenant {TenantId}: deleted Bronze={Bronze}, Silver={Silver}, Gold={Gold}",
                tenantId, bronzeDeleted, silverDeleted, goldDeleted);
        }

        return total;
    }
}
