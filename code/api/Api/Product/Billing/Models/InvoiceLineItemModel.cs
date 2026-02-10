namespace Api.Product.Billing.Models;

public class InvoiceLineItemModel
{
    public long Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string TierName { get; set; }
    public decimal UnitPrice { get; set; }
    public int DaysInPeriod { get; set; }
    public int TotalDaysInMonth { get; set; }
    public decimal Amount { get; set; }
}
