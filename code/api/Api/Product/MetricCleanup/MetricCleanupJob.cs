using Coravel.Invocable;

namespace Api.Product.MetricCleanup;

/// <summary>
/// Coravel job: Runs daily at midnight to delete expired metric data
/// based on each tenant's pricing tier data retention policy.
/// </summary>
public class MetricCleanupJob(
    IMetricCleanupService metricCleanupService,
    ILogger<MetricCleanupJob> logger) : IInvocable
{
    public async Task Invoke()
    {
        logger.LogInformation("[MetricCleanup] Daily cleanup job started");

        try
        {
            await metricCleanupService.RunCleanupAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[MetricCleanup] Daily cleanup job failed");
            throw;
        }
    }
}
