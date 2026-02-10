using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Tenancy.Entities;

namespace Api.Product.Billing.Entities;

[Table("invoice_line_items")]
public class InvoiceLineItemEntity : TenantAwareEntityLong
{
    [Required]
    public long InvoiceId { get; set; }

    [ForeignKey(nameof(InvoiceId))]
    public InvoiceEntity Invoice { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string TierName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int DaysInPeriod { get; set; }

    public int TotalDaysInMonth { get; set; }

    public decimal Amount { get; set; }
}
