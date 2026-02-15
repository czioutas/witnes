using Api.Application;
using Api.Application.Communication.Services;
using Api.Application.Pricing.Services;
using Api.Application.Tenancy.Services;
using Api.Product.Billing.Models;
using Api.Settings.Settings;
using AutoMapper;
using Libs.Result;
using Microsoft.Extensions.Options;
using Usersr.API.Users.Services;

namespace Api.Product.Billing.Services;

public interface IBillingService
{
    Task<Result<BillingRunSummary>> RunMonthlyBillingAsync(int year, int month);
}

public record BillingRunSummary(int TenantsProcessed, int InvoicesCreated, int Skipped, int Failed);

public class BillingService : IBillingService
{
    private readonly IRequestTenant _requestTenant;
    private readonly ITenantService _tenantService;
    private readonly ITenantPricingService _tenantPricingService;
    private readonly IInvoiceService _invoiceService;
    private readonly IUsersService _usersService;
    private readonly ICommunicationService _communicationService;
    private readonly ILinkResolver _linkResolver;
    private readonly SmtpSettings _smtpSettings;
    private readonly IMapper _mapper;
    private readonly ILogger<BillingService> _logger;

    public BillingService(
        IRequestTenant requestTenant,
        ITenantService tenantService,
        ITenantPricingService tenantPricingService,
        IInvoiceService invoiceService,
        IUsersService usersService,
        ICommunicationService communicationService,
        ILinkResolver linkResolver,
        IOptions<SmtpSettings> smtpSettings,
        IMapper mapper,
        ILogger<BillingService> logger)
    {
        _requestTenant = requestTenant;
        _tenantService = tenantService;
        _tenantPricingService = tenantPricingService;
        _invoiceService = invoiceService;
        _usersService = usersService;
        _communicationService = communicationService;
        _linkResolver = linkResolver;
        _smtpSettings = smtpSettings.Value;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<BillingRunSummary>> RunMonthlyBillingAsync(int year, int month)
    {
        try
        {
            _logger.LogInformation("[Billing] Starting monthly billing for {Year}-{Month:D2}", year, month);

            var tenantIds = await _tenantService.GetAllTenantIdsAsync();
            _logger.LogInformation("[Billing] Found {Count} tenants to process", tenantIds.Count);

            var invoicesCreated = 0;
            var skipped = 0;
            var failed = 0;

            foreach (var tenantId in tenantIds)
            {
                _requestTenant.SetTenantId(tenantId);
                var processResult = await ProcessTenantBillingAsync(year, month);
                if (processResult.IsFailure)
                {
                    failed++;
                    _logger.LogError("[Billing] Tenant {TenantId} failed: {Error}",
                        tenantId, processResult.ErrorModel.Message);
                    continue;
                }

                if (processResult.GetValue)
                {
                    invoicesCreated++;
                }
                else
                {
                    skipped++;
                }
            }

            _logger.LogInformation(
                "[Billing] Billing complete. Processed={Processed}, Created={Created}, Skipped={Skipped}, Failed={Failed}",
                tenantIds.Count, invoicesCreated, skipped, failed);

            return Result<BillingRunSummary>.Ok(new BillingRunSummary(tenantIds.Count, invoicesCreated, skipped, failed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Billing] Monthly billing run failed");
            return Result<BillingRunSummary>.Failure(new InternalErrorModel(ex));
        }
    }

    private async Task<Result<bool>> ProcessTenantBillingAsync(int year, int month)
    {
        try
        {
            // Idempotency check
            var invoiceExistsResult = await _invoiceService.InvoiceExistsForPeriodAsync(year, month);
            if (invoiceExistsResult.IsFailure)
            {
                return invoiceExistsResult.ToErrorResult<bool>();
            }

            if (invoiceExistsResult.GetValue)
            {
                _logger.LogDebug("[Billing] Invoice already exists for tenant {TenantId} period {Year}-{Month:D2}, skipping",
                    _requestTenant.TenantId, year, month);
                return Result<bool>.Ok(false);
            }

            var monthStart = new DateOnly(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            // Get all pricing records that overlap with the billing month
            var pricingRecordsResult = await _tenantPricingService.GetTenantPricingForPeriodAsync(monthStart, monthEnd);
            if (pricingRecordsResult.IsFailure)
            {
                return pricingRecordsResult.ToErrorResult<bool>();
            }

            var pricingRecords = pricingRecordsResult.GetValue;

            if (pricingRecords.Count == 0)
            {
                _logger.LogWarning("[Billing] Tenant {TenantId} has no pricing for period {Year}-{Month:D2}, skipping",
                    _requestTenant.TenantId, year, month);
                return Result<bool>.Ok(false);
            }

            var daysInMonth = monthEnd.DayNumber - monthStart.DayNumber + 1;
            var lineItems = new List<BillingLineItem>();
            var trialExpiredPricingIds = new List<Guid>();

            foreach (var pricing in pricingRecords)
            {
                var tier = pricing.PricingTier;
                if (tier == null) continue;

                // Calculate the segment this pricing covers within the billing month
                var segmentStart = pricing.StartDate > monthStart ? pricing.StartDate : monthStart;
                var segmentEnd = pricing.EndDate.HasValue && pricing.EndDate.Value < monthEnd ? pricing.EndDate.Value : monthEnd;
                var segmentDays = segmentEnd.DayNumber - segmentStart.DayNumber + 1;

                var chargeableDays = segmentDays;

                // Handle trial deduction
                if (pricing.HasTrial && !pricing.HasTrialExpired)
                {
                    var trialEndDate = pricing.StartDate.AddDays(6); // Last day of 7-day trial (inclusive)
                    var trialStartInSegment = pricing.StartDate > segmentStart ? pricing.StartDate : segmentStart;
                    var trialEndInSegment = trialEndDate < segmentEnd ? trialEndDate : segmentEnd;

                    if (trialStartInSegment <= trialEndInSegment)
                    {
                        var trialDaysInSegment = trialEndInSegment.DayNumber - trialStartInSegment.DayNumber + 1;
                        chargeableDays = segmentDays - trialDaysInSegment;
                    }

                    // Mark trial as expired if the full 7 days have elapsed by the end of the billing month
                    if (monthEnd >= pricing.StartDate.AddDays(7))
                    {
                        trialExpiredPricingIds.Add(pricing.Id);
                    }
                }

                // Calculate amount
                decimal amount;
                var isFullMonth = segmentStart == monthStart && segmentEnd == monthEnd;

                if (isFullMonth && chargeableDays == daysInMonth)
                {
                    // Full month, no trial deduction - use fixed monthly price
                    amount = tier.PricePerMonth;
                }
                else
                {
                    // Pro-rated
                    var dailyRate = tier.PricePerMonth / daysInMonth;
                    amount = dailyRate * chargeableDays;
                }

                amount = Math.Round(amount, 2);

                // Build description
                var description = isFullMonth && chargeableDays == daysInMonth
                    ? $"{tier.Name} Plan - {monthStart:MMMM yyyy}"
                    : $"{tier.Name} Plan - {segmentStart:MMM d} to {segmentEnd:MMM d} ({chargeableDays} days)";

                lineItems.Add(new BillingLineItem(
                    TierName: tier.Name,
                    Description: description,
                    UnitPrice: tier.PricePerMonth,
                    DaysCharged: chargeableDays,
                    TotalDaysInMonth: daysInMonth,
                    Amount: amount
                ));
            }

            // Calculate totals
            var subtotal = lineItems.Sum(li => li.Amount);
            var discountPercentage = pricingRecords.Last().DiscountPercentage;
            var discountAmount = Math.Round(subtotal * (discountPercentage / 100m), 2);
            var totalAmount = subtotal - discountAmount;

            // Get tenant details for snapshot
            var tenantResult = await _tenantService.GetAsync(_requestTenant.TenantId);
            if (tenantResult.IsFailure)
            {
                _logger.LogWarning("[Billing] Could not get tenant details for {TenantId}, skipping", _requestTenant.TenantId);
                return tenantResult.ToErrorResult<bool>();
            }

            var tenant = _mapper.Map<BillingTenantSnapshotModel>(tenantResult.GetValue);

            var calculation = new BillingCalculationResult(
            SubtotalAmount: subtotal,
            DiscountPercentage: discountPercentage,
            DiscountAmount: discountAmount,
            TotalAmount: totalAmount,
            TenantName: tenant.TenantName,
            VatNumber: tenant.VatNumber,
            CompanyRegistrationNumber: tenant.CompanyRegistrationNumber,
            StreetLine1: tenant.StreetLine1,
            StreetLine2: tenant.StreetLine2,
            City: tenant.City,
            StateProvince: tenant.StateProvince,
            PostalCode: tenant.PostalCode,
            Country: tenant.Country,
            LineItems: lineItems
        );

            // Create invoice
            var invoiceResult = await _invoiceService.CreateInvoiceAsync(year, month, calculation);
            if (invoiceResult.IsFailure)
            {
                return invoiceResult.ToErrorResult<bool>();
            }

            // Update trial expired flags
            foreach (var pricingId in trialExpiredPricingIds)
            {
                var trialResult = await _tenantPricingService.SetTrialExpiredAsync(pricingId);
                if (trialResult.IsFailure)
                {
                    return trialResult.ToErrorResult<bool>();
                }
            }

            // Send email notification
            await SendInvoiceNotificationAsync(invoiceResult.GetValue.InvoiceNumber, totalAmount, monthStart, monthEnd);

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Billing] Error processing billing for tenant {TenantId}", _requestTenant.TenantId);
            return Result<bool>.Failure(new InternalErrorModel(ex));
        }
    }

    private async Task SendInvoiceNotificationAsync(string invoiceNumber, decimal totalAmount, DateOnly periodStart, DateOnly periodEnd)
    {
        if (string.IsNullOrEmpty(_smtpSettings.InvoiceNotificationTemplateId))
        {
            _logger.LogWarning("[Billing] Invoice notification template not configured, skipping email for {InvoiceNumber}", invoiceNumber);
            return;
        }

        var adminEmail = await _usersService.GetTenantAdminEmailAsync();
        if (string.IsNullOrEmpty(adminEmail))
        {
            _logger.LogWarning("[Billing] No admin email found for tenant {TenantId}, skipping notification for {InvoiceNumber}",
                _requestTenant.TenantId, invoiceNumber);
            return;
        }

        try
        {
            var billingPageUrl = _linkResolver.GetApplicationUrl("dashboard/billing");

            // await _communicationService.SendEmailAsync(
            //     _smtpSettings.InvoiceNotificationTemplateId,
            //     adminEmail,
            //     new
            //     {
            //         invoiceNumber,
            //         totalAmount = totalAmount.ToString("0.00"),
            //         currency = "EUR",
            //         periodStart = periodStart.ToString("MMMM d, yyyy"),
            //         periodEnd = periodEnd.ToString("MMMM d, yyyy"),
            //         billingPageUrl
            //     });

            // _logger.LogInformation("[Billing] Invoice notification sent to {Email} for {InvoiceNumber}", adminEmail, invoiceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Billing] Failed to send invoice notification for {InvoiceNumber}", invoiceNumber);
        }
    }
}
