namespace Api.Application.Pricing.Models;

public class TenantPricingCacheModel
{
    public Guid TenantId { get; set; }
    public Guid PricingTierId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
    public int MonthlyPageLoads { get; set; }
    public bool HasTrial { get; set; }
    public bool HasTrialExpired { get; set; }
    public decimal DiscountPercentage { get; set; }
}
