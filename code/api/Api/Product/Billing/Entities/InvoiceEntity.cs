using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Tenancy.Entities;

namespace Api.Product.Billing.Entities;

[Table("invoices")]
public class InvoiceEntity : TenantAwareEntityLong
{
    [Required]
    [MaxLength(30)]
    public required string InvoiceNumber { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Due;

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }

    public decimal SubtotalAmount { get; set; }

    public decimal DiscountPercentage { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(255)]
    public string TenantName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? VatNumber { get; set; }

    [MaxLength(50)]
    public string? CompanyRegistrationNumber { get; set; }

    [MaxLength(255)]
    public string? StreetLine1 { get; set; }

    [MaxLength(255)]
    public string? StreetLine2 { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? StateProvince { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    public ICollection<InvoiceLineItemEntity> LineItems { get; set; } = [];
}
