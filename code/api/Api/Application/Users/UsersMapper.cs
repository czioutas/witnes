using Api.Application.Users;
using Api.Application.Users.Models;
using AutoMapper;

namespace Api.Application.Schedule;

public class UsersMapper : Profile
{
    public UsersMapper()
    {
        CreateMap<UserInvitationEntity, UserInvitationModel>();
        CreateMap<UserInvitationModel, UserInvitationEntity>();
        CreateMap<CreateUserInvitationModel, UserInvitationEntity>();
        CreateMap<CreateUserInvitationModel, UserInvitationModel>();
    }
}
