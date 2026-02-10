using Api.Application.Tenancy.Services;
using Api.Data;
using Api.Product.Billing.Entities;
using Api.Product.Billing.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Product.Billing.Services;

public interface IInvoiceService
{
    Task<InvoiceEntity> CreateInvoiceAsync(int year, int month, BillingCalculationResult calculation);
    Task<List<InvoiceModel>> GetInvoicesForTenantAsync();
    Task<InvoiceModel?> GetInvoiceAsync(long invoiceId);
    Task<bool> InvoiceExistsForPeriodAsync(int year, int month);
}

public record BillingCalculationResult(
    decimal SubtotalAmount,
    decimal DiscountPercentage,
    decimal DiscountAmount,
    decimal TotalAmount,
    string TenantName,
    string? VatNumber,
    string? CompanyRegistrationNumber,
    string? StreetLine1,
    string? StreetLine2,
    string? City,
    string? StateProvince,
    string? PostalCode,
    string? Country,
    List<BillingLineItem> LineItems
);

public record BillingLineItem(
    string TierName,
    string Description,
    decimal UnitPrice,
    int DaysCharged,
    int TotalDaysInMonth,
    decimal Amount
);

public class InvoiceService : IInvoiceService
{
    private readonly IRequestTenant _requestTenant;
    private readonly ApplicationDbContext _dbContext;
    private readonly ApplicationDbContextRead _dbContextRead;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IRequestTenant _requestTenant,
        ApplicationDbContext dbContext,
        ApplicationDbContextRead dbContextRead,
        ILogger<InvoiceService> logger)
    {
        _requestTenant = _requestTenant ?? throw new ArgumentNullException(nameof(_requestTenant));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dbContextRead = dbContextRead ?? throw new ArgumentNullException(nameof(dbContextRead));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<InvoiceEntity> CreateInvoiceAsync(int year, int month, BillingCalculationResult calculation)
    {
        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var invoice = new InvoiceEntity
        {
            InvoiceNumber = $"INV-{year}",
            Status = InvoiceStatus.Due,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            SubtotalAmount = calculation.SubtotalAmount,
            DiscountPercentage = calculation.DiscountPercentage,
            DiscountAmount = calculation.DiscountAmount,
            TotalAmount = calculation.TotalAmount,
            TenantName = calculation.TenantName,
            VatNumber = calculation.VatNumber,
            CompanyRegistrationNumber = calculation.CompanyRegistrationNumber,
            StreetLine1 = calculation.StreetLine1,
            StreetLine2 = calculation.StreetLine2,
            City = calculation.City,
            StateProvince = calculation.StateProvince,
            PostalCode = calculation.PostalCode,
            Country = calculation.Country,
        };

        foreach (var lineItem in calculation.LineItems)
        {
            invoice.LineItems.Add(new InvoiceLineItemEntity
            {
                Description = lineItem.Description,
                TierName = lineItem.TierName,
                UnitPrice = lineItem.UnitPrice,
                DaysInPeriod = lineItem.DaysCharged,
                TotalDaysInMonth = lineItem.TotalDaysInMonth,
                Amount = lineItem.Amount,
            });
        }

        _dbContext.Invoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        // Now we have the DB-generated Id, update the invoice number
        invoice.InvoiceNumber = $"INV-{year}-{invoice.Id}";
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("[Billing] Created invoice {InvoiceNumber} for tenant {TenantId}, amount: {Amount}",
            invoice.InvoiceNumber, _requestTenant.TenantId, invoice.TotalAmount);

        return invoice;
    }

    public async Task<List<InvoiceModel>> GetInvoicesForTenantAsync()
    {
        var invoices = await _dbContextRead.Invoices
            .Include(i => i.LineItems)
            .OrderByDescending(i => i.PeriodStart)
            .ToListAsync();

        return invoices.Select(MapToModel).ToList();
    }

    public async Task<InvoiceModel?> GetInvoiceAsync(long invoiceId)
    {
        var invoice = await _dbContextRead.Invoices
            .Include(i => i.LineItems)
            .Where(i => i.Id == invoiceId)
            .FirstOrDefaultAsync();

        return invoice != null ? MapToModel(invoice) : null;
    }

    public async Task<bool> InvoiceExistsForPeriodAsync(int year, int month)
    {
        var periodStart = new DateOnly(year, month, 1);

        return await _dbContextRead.Invoices
            .AnyAsync(i => i.PeriodStart == periodStart);
    }

    private static InvoiceModel MapToModel(InvoiceEntity entity)
    {
        return new InvoiceModel
        {
            Id = entity.Id,
            InvoiceNumber = entity.InvoiceNumber,
            Status = entity.Status,
            PeriodStart = entity.PeriodStart,
            PeriodEnd = entity.PeriodEnd,
            SubtotalAmount = entity.SubtotalAmount,
            DiscountPercentage = entity.DiscountPercentage,
            DiscountAmount = entity.DiscountAmount,
            TotalAmount = entity.TotalAmount,
            TenantName = entity.TenantName,
            VatNumber = entity.VatNumber,
            CompanyRegistrationNumber = entity.CompanyRegistrationNumber,
            StreetLine1 = entity.StreetLine1,
            StreetLine2 = entity.StreetLine2,
            City = entity.City,
            StateProvince = entity.StateProvince,
            PostalCode = entity.PostalCode,
            Country = entity.Country,
            CreatedAt = entity.CreatedAt,
            LineItems = entity.LineItems.Select(li => new InvoiceLineItemModel
            {
                Id = li.Id,
                Description = li.Description,
                TierName = li.TierName,
                UnitPrice = li.UnitPrice,
                DaysInPeriod = li.DaysInPeriod,
                TotalDaysInMonth = li.TotalDaysInMonth,
                Amount = li.Amount,
            }).ToList()
        };
    }
}
