using Api.Application.Account.Entities;
using Api.Application.Account.Models;
using AutoMapper;

namespace Api.Application.Account;

public class AccountMapper : Profile
{
    public AccountMapper()
    {
        CreateMap<ApplicationUserModel, ApplicationUserEntity>();
        CreateMap<ApplicationUserEntity, ApplicationUserModel>();
        CreateMap<ApplicationUserEntity, SlimApplicationUserModel>()
            .ForMember(dest => dest.TenantName, opt => opt.MapFrom(src => src.Tenant!.Identifier))
            .ForMember(dest => dest.Roles, opt => opt.Ignore());
    }
}
