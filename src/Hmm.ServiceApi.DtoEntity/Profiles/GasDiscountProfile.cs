using AutoMapper;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.Utility.Currency;

namespace Hmm.ServiceApi.DtoEntity.Profiles
{
    public class GasDiscountProfile : Profile
    {
        public GasDiscountProfile()
        {
            CreateMap<GasDiscount, ApiDiscount>()
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount));

            CreateMap<ApiDiscountForCreate, GasDiscount>()
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => new Money(src.Amount, CurrencyCodeType.Cad)));
        }
    }
}