using AutoMapper;
using Hmm.ServiceApi.DtoEntity.Utility;
using Hmm.Utility.Services;

namespace Hmm.ServiceApi.Areas.UtilityService.Infrastructure
{
    public class UtilityServiceMappingProfile : Profile
    {
        public UtilityServiceMappingProfile()
        {
            CreateMap<GeoAddress, ApiGeoAddress>();
        }
    }
}
