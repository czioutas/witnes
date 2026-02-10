using Api.Product.Billing.Entities;

namespace Api.Product.Billing.Models;

public class InvoiceModel
{
    public long Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string? VatNumber { get; set; }
    public string? CompanyRegistrationNumber { get; set; }
    public string? StreetLine1 { get; set; }
    public string? StreetLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<InvoiceLineItemModel> LineItems { get; set; } = new();
}
