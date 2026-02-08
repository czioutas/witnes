using Api.Application.Limits.Jobs;
using Coravel;

namespace Api.Application.Extensions;

public static class SchedulerExtensions
{
    public static void ScheduleJobs(this IServiceProvider serviceProvider)
    {
        serviceProvider.UseScheduler(scheduler =>
        {
        });
    }
}
