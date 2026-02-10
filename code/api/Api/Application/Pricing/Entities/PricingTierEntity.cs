using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Application.Entities;

namespace Api.Application.Pricing.Entities;

[Table("pricing_tiers")]
public class PricingTierEntity : AuditableBaseEntity
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    public int MonthlyPageLoads { get; set; }

    public decimal PricePerMonth { get; set; }

    public int MaxTeamMembers { get; set; }

    public int DataRetentionDays { get; set; }

    public bool IsActive { get; set; } = true;

    public PricingTierEntity() { }

    public PricingTierEntity(
        string name,
        string description,
        int monthlyPageLoads,
        decimal pricePerMonth,
        int maxTeamMembers,
        int dataRetentionDays)
    {
        Name = name;
        Description = description;
        MonthlyPageLoads = monthlyPageLoads;
        PricePerMonth = pricePerMonth;
        MaxTeamMembers = maxTeamMembers;
        DataRetentionDays = dataRetentionDays;
    }
}
