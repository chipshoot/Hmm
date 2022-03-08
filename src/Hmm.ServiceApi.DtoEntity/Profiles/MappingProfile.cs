using AutoMapper;
using Hmm.Automobile.DomainEntity;
using Hmm.Core.DomainEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.Utility.Currency;
using Hmm.Utility.MeasureUnit;
using System;
using System.Linq;

namespace Hmm.ServiceApi.DtoEntity.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Author
            CreateMap<ApiAuthor, Author>()
                .ForMember(d => d.Role, opt => opt.MapFrom(src => Enum.Parse(typeof(AuthorRoleType), src.Role, true)));
            CreateMap<Author, ApiAuthor>()
                .ForMember(d => d.Role, opt => opt.MapFrom(src => src.Role.ToString().ToLower()));
            CreateMap<ApiAuthorForCreate, Author>();
            CreateMap<ApiAuthorForUpdate, Author>();
            CreateMap<Author, ApiAuthorForUpdate>();

            // Render
            CreateMap<ApiNoteRender, NoteRender>();
            CreateMap<NoteRender, ApiNoteRender>();
            CreateMap<ApiNoteRenderForCreate, NoteRender>();
            CreateMap<ApiNoteRenderForUpdate, NoteRender>();
            CreateMap<NoteRender, ApiNoteRenderForUpdate>();

            // Catalog
            CreateMap<ApiNoteCatalog, NoteCatalog>();
            CreateMap<NoteCatalog, ApiNoteCatalog>()
                .ForMember(c => c.SubsystemId, opt => opt.MapFrom(s => s.Subsystem.Id))
                .ForMember(c => c.RenderId, opt => opt.MapFrom(s => s.Render.Id));

            CreateMap<ApiNoteCatalogForCreate, NoteCatalog>();
            CreateMap<ApiNoteCatalogForUpdate, NoteCatalog>();
            CreateMap<NoteCatalog, ApiNoteCatalogForUpdate>();

            // Subsystem
            CreateMap<Subsystem, ApiSubsystem>()
                .ForMember(t => t.DefaultAuthorId, opt => opt.MapFrom(s => s.DefaultAuthor.Id))
                .ForMember(t => t.NoteCatalogIds, opt => opt.MapFrom(s => s.NoteCatalogs.Select(c => c.Id).ToList()));
            CreateMap<ApiSubsystem, Subsystem>();
            CreateMap<ApiSubsystemForUpdate, Subsystem>();
            CreateMap<Subsystem, ApiSubsystemForUpdate>();

            // Note
            CreateMap<Core.DomainEntity.HmmNote, ApiNote>()
                .ForMember(n => n.AuthorId, opt => opt.MapFrom(s => s.Author.Id))
                .ForMember(n => n.CatalogId, opt => opt.MapFrom(s => s.Catalog.Id));

            CreateMap<ApiNoteForCreate, Core.DomainEntity.HmmNote>()
                .ForMember(n => n.Author, opt => opt.MapFrom(s => new Author { Id = s.AuthorId }))
                .ForMember(n => n.Catalog, opt => opt.MapFrom(s => new NoteCatalog() { Id = s.NoteCatalogId }));
            CreateMap<ApiNoteForUpdate, Core.DomainEntity.HmmNote>();
            CreateMap<Core.DomainEntity.HmmNote, ApiNoteForUpdate>();

            // Automobile
            CreateMap<ApiAutomobile, AutomobileInfo>();
            CreateMap<AutomobileInfo, ApiAutomobile>();
            CreateMap<ApiAutomobileForCreate, AutomobileInfo>();
            CreateMap<ApiAutomobileForUpdate, AutomobileInfo>();
            CreateMap<AutomobileInfo, ApiAutomobileForUpdate>();

            // Gas Discount: setup map from domain entity to DTO
            CreateMap<GasDiscount, ApiDiscount>()
                .ForMember(d => d.Amount, opt => opt.MapFrom(s => s.Amount.Amount));
            CreateMap<GasDiscountInfo, ApiDiscountInfo>()
                .ForMember(d => d.DiscountId, opt => opt.MapFrom(s => s.Program.Id))
                .ForMember(d => d.Amount, opt => opt.MapFrom(s => s.Amount.Amount));
            CreateMap<ApiDiscountForCreate, GasDiscount>()
                .ForMember(d => d.Amount, opt => opt.MapFrom(src => new Money(src.Amount, CurrencyCodeType.Cad)));
            CreateMap<ApiDiscountForUpdate, GasDiscount>()
                .ForMember(d => d.Amount, opt => opt.MapFrom(src => new Money(src.Amount, CurrencyCodeType.Cad)));
            CreateMap<GasDiscount, ApiDiscountForUpdate>()
                .ForMember(d => d.Amount, opt => opt.MapFrom(src => src.Amount.Amount));

            // Gas Log
            CreateMap<GasLog, ApiGasLog>()
                .ForMember(d => d.CarId, opt => opt.MapFrom(s => s.Car.Id))
                .ForMember(d => d.Distance, opt => opt.MapFrom(s => s.Distance.TotalKilometre))
                .ForMember(d => d.CurrentMeterReading, opt => opt.MapFrom(s => s.CurrentMeterReading.TotalKilometre))
                .ForMember(d => d.Gas, opt => opt.MapFrom(s => s.Gas.TotalLiter))
                .ForMember(d => d.Price, opt => opt.MapFrom(s => s.Price.Amount))
                .ForMember(d => d.DiscountInfos, opt => opt.MapFrom(s => s.Discounts))
                .ForMember(d => d.GasStation, opt => opt.MapFrom(s => s.Station));
            CreateMap<ApiGasLogForCreation, GasLog>()
                .ForMember(d => d.Distance, opt => opt.MapFrom(s => Dimension.FromKilometer(s.Distance)))
                .ForMember(d => d.CurrentMeterReading, opt => opt.MapFrom(s => Dimension.FromKilometer(s.CurrentMeterReading)))
                .ForMember(d => d.Gas, opt => opt.MapFrom(s => Volume.FromLiter(s.Gas)))
                .ForMember(d => d.Price, opt => opt.MapFrom(s => new Money(s.Price)))
                .ForMember(d => d.Discounts, opt => opt.Ignore())
                .ForMember(d => d.Car, opt => opt.Ignore())
                .ForMember(d => d.Station, opt => opt.MapFrom(src => src.GasStation));
            CreateMap<ApiGasLog, GasLog>()
                .ForMember(d => d.Car, opt => opt.MapFrom(src => new AutomobileInfo { Id = src.CarId }))
                .ForMember(d => d.Distance, opt => opt.MapFrom(src => Dimension.FromKilometer(src.Distance)))
                .ForMember(d => d.CurrentMeterReading, opt => opt.MapFrom(src => Dimension.FromKilometer(src.CurrentMeterReading)))
                .ForMember(d => d.Gas, opt => opt.MapFrom((src => Volume.FromLiter(src.Gas))))
                .ForMember(d => d.Price, opt => opt.MapFrom(src => new Money(src.Price)))
                .ForMember(d => d.Station, opt => opt.MapFrom(src => src.GasStation));
        }
    }
}