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
    private readonly IRequestTenant _requestTenant;

    public MetricCleanupService(
        IRequestTenant requestTenant,
        ITenantService tenantService,
        ITenantPricingService tenantPricingService,
        IBronzeService bronzeService,
        ISilverService silverService,
        IGoldService goldService,
        TimeProvider timeProvider,
        ILogger<MetricCleanupService> logger)
    {
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _tenantPricingService = tenantPricingService ?? throw new ArgumentNullException(nameof(tenantPricingService));
        _bronzeService = bronzeService ?? throw new ArgumentNullException(nameof(bronzeService));
        _silverService = silverService ?? throw new ArgumentNullException(nameof(silverService));
        _goldService = goldService ?? throw new ArgumentNullException(nameof(goldService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                _requestTenant.SetTenantId(tenantId);
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
        var pricingResult = await _tenantPricingService.GetCurrentTenantPricingAsync();
        if (pricingResult.IsFailure)
        {
            _logger.LogWarning("[MetricCleanup] Tenant {TenantId} has no active pricing tier, skipping", tenantId);
            return 0;
        }

        var pricing = pricingResult.GetValue;
        if (pricing.PricingTier == null)
        {
            _logger.LogWarning("[MetricCleanup] Tenant {TenantId} has invalid pricing tier configuration, skipping", tenantId);
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
