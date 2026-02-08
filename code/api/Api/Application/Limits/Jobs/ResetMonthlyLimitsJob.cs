using Api.Data;
using Coravel.Invocable;
using Libs.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Limits.Jobs;

/// <summary>
/// Coravel job: Resets monthly limits for all tenants on the first day of each month.
/// Resets CurrentAmount to 0 and updates LastResetAt timestamp.
/// </summary>
public class ResetMonthlyLimitsJob(
    ApplicationDbContext context,
    TimeProvider timeProvider,
    ILogger<ResetMonthlyLimitsJob> logger) : IInvocable
{
    public async Task Invoke()
    {
        logger.LogInformation("[Limits] Monthly limit reset job started");

        try
        {
            var now = timeProvider.GetUtcNow();

            // Get all monthly limits (ignore query filters to access all tenants)
            var monthlyLimits = await context.TenantLimits
                .IgnoreQueryFilters()
                .Where(l => l.Period == LimitPeriod.Monthly)
                .ToListAsync();

            if (monthlyLimits.Count == 0)
            {
                logger.LogInformation("[Limits] No monthly limits found to reset");
                return;
            }

            logger.LogInformation("[Limits] Resetting {Count} monthly limits", monthlyLimits.Count);

            var resetCount = 0;
            foreach (var limit in monthlyLimits)
            {
                // Reset the current amount to 0
                limit.CurrentAmount = 0;
                limit.LastResetAt = now.DateTime;
                resetCount++;
            }

            await context.SaveChangesAsync();

            logger.LogInformation("[Limits] Successfully reset {Count} monthly limits", resetCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Limits] Error resetting monthly limits");
            throw;
        }
    }
}
