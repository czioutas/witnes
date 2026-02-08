using Api.Application.Tenancy.Entities;
using Api.Application.Tenancy.Models;
using AutoMapper;

namespace Api.Application.Status;

public class TenantMapper : Profile
{
    public TenantMapper()
    {
        CreateMap<TenantModel, TenantEntity>();
        CreateMap<TenantEntity, TenantModel>();
    }
}
