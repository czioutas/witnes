using Api.Product.Billing.Services;
using Coravel.Invocable;

namespace Api.Product.Billing;

/// <summary>
/// Coravel job: Runs on the 1st of each month to process billing for the previous month.
/// </summary>
public class BillingJob(
    TimeProvider timeProvider,
    IBillingService billingService,
    ILogger<BillingJob> logger) : IInvocable
{
    public async Task Invoke()
    {
        logger.LogInformation("[Billing] Monthly billing job started");

        try
        {
            var now = timeProvider.GetUtcNow();
            // Bill for the previous month
            var billingMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
            var result = await billingService.RunMonthlyBillingAsync(billingMonth.Year, billingMonth.Month);
            if (result.IsFailure)
            {
                logger.LogError("[Billing] Monthly billing run failed: {Error}", result.ErrorModel.Message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Billing] Monthly billing job failed");
            throw;
        }
    }
}
