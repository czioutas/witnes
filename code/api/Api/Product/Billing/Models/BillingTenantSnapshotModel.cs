namespace Api.Product.Billing.Models;

public class BillingTenantSnapshotModel
{
    public string TenantName { get; set; } = string.Empty;
    public string? VatNumber { get; set; }
    public string? CompanyRegistrationNumber { get; set; }
    public string? StreetLine1 { get; set; }
    public string? StreetLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}
