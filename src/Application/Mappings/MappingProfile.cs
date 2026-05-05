using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Add maps if needed
        CreateMap<User, User>();
    }
}
