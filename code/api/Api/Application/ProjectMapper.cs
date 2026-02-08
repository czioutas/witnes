using Api.Application.Entities;
using Api.Application.Models;
using AutoMapper;

namespace Api.Application;

public class ProjectMapper : Profile
{
    public ProjectMapper()
    {
        CreateMap<ManagedFileEntity, ManagedFileModel>();
        CreateMap<PublicFileEntity, PublicFileModel>();
    }
}
