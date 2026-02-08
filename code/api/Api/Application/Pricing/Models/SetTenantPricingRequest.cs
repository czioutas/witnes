using System.ComponentModel.DataAnnotations;

namespace Api.Application.Pricing.Models;

public class SetTenantPricingRequest
{
    [Required]
    public Guid PricingTierId { get; set; }

    public DateOnly? StartDate { get; set; }
}
