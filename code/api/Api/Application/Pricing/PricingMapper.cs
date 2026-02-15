using Api.Application.Pricing.Entities;
using Api.Application.Pricing.Models;
using AutoMapper;

namespace Api.Application.Pricing;

public class PricingMapper : Profile
{
    public PricingMapper()
    {
        CreateMap<TenantPricingTierEntity, TenantPricingCacheModel>()
            .ForMember(dest => dest.MonthlyPageLoads,
                opt => opt.MapFrom(src => src.PricingTier != null ? src.PricingTier.MonthlyPageLoads : 0));
    }
}
