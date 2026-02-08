using System.ComponentModel.DataAnnotations;

namespace Api.Application.Pricing.Models;

public class ChangeTenantPricingRequest
{
    [Required]
    public Guid NewPricingTierId { get; set; }

    public DateOnly? StartDate { get; set; }
}
