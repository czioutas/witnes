using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Tenancy.Entities;

namespace Api.Application.Pricing.Entities;

[Table("tenant_pricing_tiers")]
public class TenantPricingTierEntity : TenantAwareEntity
{
    [Required]
    public Guid PricingTierId { get; set; }

    [ForeignKey(nameof(PricingTierId))]
    public PricingTierEntity PricingTier { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public TenantPricingTierEntity() { }

    public TenantPricingTierEntity(
        Guid pricingTierId,
        DateOnly startDate)
    {
        PricingTierId = pricingTierId;
        StartDate = startDate;
    }
}
