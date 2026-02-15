using Api.Application.Tenancy.Models;
using Api.Product.Billing.Entities;
using Api.Product.Billing.Models;
using Api.Product.Billing.Services;
using AutoMapper;

namespace Api.Product.Billing;

public class BillingMapper : Profile
{
    public BillingMapper()
    {
        CreateMap<InvoiceEntity, InvoiceModel>();
        CreateMap<InvoiceLineItemEntity, InvoiceLineItemModel>();

        CreateMap<BillingLineItem, InvoiceLineItemEntity>()
            .ForMember(dest => dest.DaysInPeriod, opt => opt.MapFrom(src => src.DaysCharged));

        CreateMap<TenantModel, BillingTenantSnapshotModel>()
            .ForMember(dest => dest.TenantName, opt => opt.MapFrom(src => src.Identifier));
    }
}
