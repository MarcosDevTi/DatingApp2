using System;
using System.Linq;
using AutoMapper;
using DatingApp.API.DTOs;
using DatingApp.API.Models;

namespace DatingApp.API.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<User, UserForListDto>()
                .ForMember(dest=> dest.Age, opt => opt.MapFrom(source => source.DateOfBirth.CalculateAge()))
                .ForMember(x => x.PhotoUrl, opt => opt.MapFrom(s =>
                        s.Photos.FirstOrDefault(p => p.IsMain).Url));

            CreateMap<User, UserForDetailedDto>()
                .ForMember(dest=> dest.Age, opt => opt.MapFrom(source => 
                        source.DateOfBirth.CalculateAge()))
               .ForMember(dest => dest.PhotoUrl, opt => 
                        opt.MapFrom(source =>source.Photos.FirstOrDefault(p => p.IsMain).Url));

            CreateMap<Photo, PhotosForDetailedDto>();
        }
    }
}