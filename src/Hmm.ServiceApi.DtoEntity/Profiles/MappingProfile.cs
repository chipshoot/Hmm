using AutoMapper;
using Hmm.Automobile.DomainEntity;
using Hmm.Core.DomainEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.Utility.Currency;
using Hmm.Utility.MeasureUnit;
using System;

namespace Hmm.ServiceApi.DtoEntity.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ApiAuthor, Author>()
                .ForMember(d => d.Role, opt => opt.MapFrom(src => Enum.Parse(typeof(AuthorRoleType), src.Role, true)));
            CreateMap<Author, ApiAuthor>()
                .ForMember(d => d.Role, opt => opt.MapFrom(src => src.Role.ToString().ToLower()));
            CreateMap<ApiAuthorForCreate, Author>();
            CreateMap<ApiAuthorForUpdate, Author>();
            CreateMap<Author, ApiAuthorForUpdate>();

            CreateMap<ApiNoteRender, NoteRender>();
            CreateMap<NoteRender, ApiNoteRender>();
            CreateMap<ApiNoteRenderForCreate, NoteRender>();
            CreateMap<ApiNoteRenderForUpdate, NoteRender>();
            CreateMap<NoteRender, ApiNoteRenderForUpdate>();
            CreateMap<ApiSubsystem, Subsystem>();

            CreateMap<ApiNoteCatalog, NoteCatalog>();
            CreateMap<NoteCatalog, ApiNoteCatalog>();
            CreateMap<ApiNoteCatalogForCreate, NoteCatalog>();
            CreateMap<ApiNoteCatalogForUpdate, NoteCatalog>();
            CreateMap<NoteCatalog, ApiNoteCatalogForUpdate>();
            CreateMap<Subsystem, ApiSubsystem>();

            // gas log: setup map from domain entity to DTO
            CreateMap<GasDiscount, ApiDiscount>()
                .ForMember(d => d.Amount, opt => opt.MapFrom(s => s.Amount.Amount));
            CreateMap<GasDiscountInfo, ApiDiscountInfo>()
                .ForMember(d => d.DiscountId, opt => opt.MapFrom(s => s.Program.Id))
                .ForMember(d => d.Amount, opt => opt.MapFrom(s => s.Amount.Amount));
            CreateMap<GasLog, ApiGasLog>()
                .ForMember(d => d.CarId, opt => opt.MapFrom(s => s.Car.Id))
                .ForMember(d => d.Distance, opt => opt.MapFrom(s => s.Distance.TotalKilometre))
                .ForMember(d => d.CurrentMeterReading, opt => opt.MapFrom(s => s.CurrentMeterReading.TotalKilometre))
                .ForMember(d => d.Gas, opt => opt.MapFrom(s => s.Gas.TotalLiter))
                .ForMember(d => d.Price, opt => opt.MapFrom(s => s.Price.Amount))
                .ForMember(d => d.DiscountInfos, opt => opt.MapFrom(s => s.Discounts));

            // gas log: setup map from DTO to domain entity
            CreateMap<ApiDiscountForCreate, GasDiscount>()
                .ForMember(d => d.Amount, opt => opt.MapFrom(src => new Money(src.Amount, CurrencyCodeType.Cad)));
            CreateMap<ApiDiscountForUpdate, GasDiscount>()
                .ForMember(d => d.Amount, opt => opt.MapFrom(src => new Money(src.Amount, CurrencyCodeType.Cad)));
            CreateMap<ApiAutomobile, AutomobileInfo>();
            CreateMap<ApiAutomobileForCreate, AutomobileInfo>();
            CreateMap<ApiGasLogForCreation, GasLog>()
                .ForMember(d => d.Distance, opt => opt.MapFrom(s => Dimension.FromKilometer(s.Distance)))
                .ForMember(d => d.CurrentMeterReading, opt => opt.MapFrom(s => Dimension.FromKilometer(s.CurrentMetterReading)))
                .ForMember(d => d.Gas, opt => opt.MapFrom(s => Volume.FromLiter(s.Gas)))
                .ForMember(d => d.Price, opt => opt.MapFrom(s => new Money(s.Price)))
                .ForMember(d => d.Discounts, opt => opt.Ignore())
                .ForMember(d => d.Car, opt => opt.Ignore());
            CreateMap<ApiGasLog, GasLog>()
                .ForMember(d => d.Car, opt => opt.MapFrom((src => new AutomobileInfo { Id = src.CarId })))
                .ForMember(d => d.Distance, opt => opt.MapFrom((src => Dimension.FromKilometer(src.Distance))))
                .ForMember(d => d.CurrentMeterReading, opt => opt.MapFrom((src => Dimension.FromKilometer(src.CurrentMeterReading))))
                .ForMember(d => d.Gas, opt => opt.MapFrom((src => Volume.FromLiter(src.Gas))))
                .ForMember(d => d.Price, opt => opt.MapFrom((src => new Money(src.Price))));
        }
    }
}