using Api.Product.Billing;
using Api.Product.MetricCleanup;
using Coravel;

namespace Api.Application.Extensions;

public static class SchedulerExtensions
{
    public static void ScheduleJobs(this IServiceProvider serviceProvider)
    {
        serviceProvider.UseScheduler(scheduler =>
        {
            scheduler.Schedule<MetricCleanupJob>()
                .DailyAtHour(0);

            scheduler.Schedule<BillingJob>()
                .Cron("0 6 1 * *"); // 1st of each month at midnight
        });
    }
}
