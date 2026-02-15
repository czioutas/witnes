using Api.Application.Tenancy.Services;
using Api.Data;
using Api.Product.Billing.Entities;
using Api.Product.Billing.Models;
using AutoMapper;
using Libs.Result;
using Microsoft.EntityFrameworkCore;

namespace Api.Product.Billing.Services;

public interface IInvoiceService
{
    Task<Result<InvoiceEntity>> CreateInvoiceAsync(int year, int month, BillingCalculationResult calculation);
    Task<Result<List<InvoiceModel>>> GetInvoicesForTenantAsync();
    Task<Result<InvoiceModel>> GetInvoiceAsync(long invoiceId);
    Task<Result<bool>> InvoiceExistsForPeriodAsync(int year, int month);
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
    private readonly IMapper _mapper;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IRequestTenant requestTenant,
        ApplicationDbContext dbContext,
        ApplicationDbContextRead dbContextRead,
        IMapper mapper,
        ILogger<InvoiceService> logger)
    {
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dbContextRead = dbContextRead ?? throw new ArgumentNullException(nameof(dbContextRead));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<InvoiceEntity>> CreateInvoiceAsync(int year, int month, BillingCalculationResult calculation)
    {
        try
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
                LineItems = _mapper.Map<List<InvoiceLineItemEntity>>(calculation.LineItems)
            };

            _dbContext.Invoices.Add(invoice);
            await _dbContext.SaveChangesAsync();

            // Now we have the DB-generated Id, update the invoice number
            invoice.InvoiceNumber = $"INV-{year}-{invoice.Id}";
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("[Billing] Created invoice {InvoiceNumber} for tenant {TenantId}, amount: {Amount}",
                invoice.InvoiceNumber, _requestTenant.TenantId, invoice.TotalAmount);

            return Result<InvoiceEntity>.Ok(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Billing] Failed to create invoice for tenant {TenantId}", _requestTenant.TenantId);
            return Result<InvoiceEntity>.Failure(new InternalErrorModel(ex));
        }
    }

    public async Task<Result<List<InvoiceModel>>> GetInvoicesForTenantAsync()
    {
        try
        {
            var invoices = await _dbContextRead.Invoices
                .Include(i => i.LineItems)
                .OrderByDescending(i => i.PeriodStart)
                .ToListAsync();

            return Result<List<InvoiceModel>>.Ok(_mapper.Map<List<InvoiceModel>>(invoices));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Billing] Failed to get invoices for tenant {TenantId}", _requestTenant.TenantId);
            return Result<List<InvoiceModel>>.Failure(new InternalErrorModel(ex));
        }
    }

    public async Task<Result<InvoiceModel>> GetInvoiceAsync(long invoiceId)
    {
        try
        {
            var invoice = await _dbContextRead.Invoices
                .Include(i => i.LineItems)
                .Where(i => i.Id == invoiceId)
                .FirstOrDefaultAsync();

            if (invoice == null)
            {
                return Result<InvoiceModel>.NotFound("Invoice", invoiceId.ToString());
            }

            return Result<InvoiceModel>.Ok(_mapper.Map<InvoiceModel>(invoice));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Billing] Failed to get invoice {InvoiceId} for tenant {TenantId}", invoiceId, _requestTenant.TenantId);
            return Result<InvoiceModel>.Failure(new InternalErrorModel(ex));
        }
    }

    public async Task<Result<bool>> InvoiceExistsForPeriodAsync(int year, int month)
    {
        try
        {
            var periodStart = new DateOnly(year, month, 1);
            var exists = await _dbContextRead.Invoices
                .AnyAsync(i => i.PeriodStart == periodStart);
            return Result<bool>.Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Billing] Failed to check invoice existence for tenant {TenantId}, period {Year}-{Month:D2}",
                _requestTenant.TenantId, year, month);
            return Result<bool>.Failure(new InternalErrorModel(ex));
        }
    }
}
