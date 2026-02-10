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
        });
    }
}
