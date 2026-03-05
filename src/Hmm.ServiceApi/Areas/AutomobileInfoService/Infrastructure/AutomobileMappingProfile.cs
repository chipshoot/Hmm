using AutoMapper;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using GeoAddress = Hmm.Automobile.DomainEntity.GeoAddress;
using Hmm.ServiceApi.DtoEntity.Profiles;
using Hmm.Utility.Currency;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.MeasureUnit;
using System;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Infrastructure
{
    public class AutomobileMappingProfile : Profile
    {
        private static DimensionUnit ParseDimensionUnit(string unit)
        {
            if (string.IsNullOrEmpty(unit))
                return DimensionUnit.Kilometre;

            // Handle American spelling from Flutter client
            return unit switch
            {
                "Kilometer" => DimensionUnit.Kilometre,
                "Mile" => DimensionUnit.Mile,
                _ => Enum.TryParse<DimensionUnit>(unit, true, out var parsed)
                    ? parsed
                    : DimensionUnit.Kilometre
            };
        }

        private static VolumeUnit ParseVolumeUnit(string unit)
        {
            if (string.IsNullOrEmpty(unit))
                return VolumeUnit.Gallon;

            return Enum.TryParse<VolumeUnit>(unit, true, out var parsed)
                ? parsed
                : VolumeUnit.Gallon;
        }

        private static CurrencyCodeType ParseCurrency(string currency)
        {
            if (string.IsNullOrEmpty(currency))
                return CurrencyCodeType.Cad;

            return Enum.TryParse<CurrencyCodeType>(currency, true, out var parsed)
                ? parsed
                : CurrencyCodeType.Cad;
        }

        public AutomobileMappingProfile()
        {
            // AutomobileInfo mappings
            CreateMap<AutomobileInfo, ApiAutomobile>()
                .ForMember(d => d.EngineType, opt => opt.MapFrom(s => s.EngineType.ToString()))
                .ForMember(d => d.FuelType, opt => opt.MapFrom(s => s.FuelType.ToString()))
                .ForMember(d => d.OwnershipStatus, opt => opt.MapFrom(s => s.OwnershipStatus.ToString()));

            CreateMap<ApiAutomobileForCreate, AutomobileInfo>()
                .ForMember(d => d.EngineType, opt => opt.MapFrom(s => Enum.Parse<FuelEngineType>(s.EngineType, true)))
                .ForMember(d => d.FuelType, opt => opt.MapFrom(s => Enum.Parse<FuelGrade>(s.FuelType, true)))
                .ForMember(d => d.OwnershipStatus, opt => opt.MapFrom(s =>
                    string.IsNullOrEmpty(s.OwnershipStatus) ? OwnershipType.Owned : Enum.Parse<OwnershipType>(s.OwnershipStatus, true)))
                .ForMember(d => d.IsActive, opt => opt.MapFrom(_ => true))
                .ForMember(d => d.CreatedDate, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(d => d.LastModifiedDate, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<ApiAutomobileForUpdate, AutomobileInfo>()
                .ForMember(d => d.OwnershipStatus, opt => opt.MapFrom(s =>
                    string.IsNullOrEmpty(s.OwnershipStatus) ? OwnershipType.Owned : Enum.Parse<OwnershipType>(s.OwnershipStatus, true)))
                .ForMember(d => d.LastModifiedDate, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<AutomobileInfo, ApiAutomobileForUpdate>()
                .ForMember(d => d.OwnershipStatus, opt => opt.MapFrom(s => s.OwnershipStatus.ToString()));

            CreateMap<PageList<AutomobileInfo>, PageList<ApiAutomobile>>()
                .ConvertUsing(new PageListConverter<AutomobileInfo, ApiAutomobile>());

            // GasLog mappings
            CreateMap<GasLog, ApiGasLog>()
                .ForMember(d => d.AutomobileId, opt => opt.MapFrom(s => s.AutomobileId))
                .ForMember(d => d.Odometer, opt => opt.MapFrom(s => (decimal)s.Odometer.Value))
                .ForMember(d => d.OdometerUnit, opt => opt.MapFrom(s => s.Odometer.Unit.ToString()))
                .ForMember(d => d.Distance, opt => opt.MapFrom(s => (decimal)s.Distance.Value))
                .ForMember(d => d.DistanceUnit, opt => opt.MapFrom(s => s.Distance.Unit.ToString()))
                .ForMember(d => d.Fuel, opt => opt.MapFrom(s => (decimal)s.Fuel.Value))
                .ForMember(d => d.FuelUnit, opt => opt.MapFrom(s => s.Fuel.Unit.ToString()))
                .ForMember(d => d.FuelGrade, opt => opt.MapFrom(s => s.FuelGrade.ToString()))
                .ForMember(d => d.TotalPrice, opt => opt.MapFrom(s => s.TotalPrice.Amount))
                .ForMember(d => d.UnitPrice, opt => opt.MapFrom(s => s.UnitPrice.Amount))
                .ForMember(d => d.Currency, opt => opt.MapFrom(s => s.TotalPrice.Currency.ToString()))
                .ForMember(d => d.TotalCostAfterDiscounts, opt => opt.MapFrom(s => s.TotalCostAfterDiscounts.Amount))
                .ForMember(d => d.StationName, opt => opt.MapFrom(s => s.Station != null ? s.Station.Name : null));

            CreateMap<ApiGasLogForCreation, GasLog>()
                .ForMember(d => d.AutomobileId, opt => opt.MapFrom(s => s.AutomobileId))
                .ForMember(d => d.Odometer, opt => opt.MapFrom(s => new Dimension((double)s.Odometer, ParseDimensionUnit(s.OdometerUnit), 3)))
                .ForMember(d => d.Distance, opt => opt.MapFrom(s => new Dimension((double)s.Distance, ParseDimensionUnit(s.DistanceUnit), 3)))
                .ForMember(d => d.Fuel, opt => opt.MapFrom(s => new Volume((double)s.Fuel, ParseVolumeUnit(s.FuelUnit), 3)))
                .ForMember(d => d.FuelGrade, opt => opt.MapFrom(s => Enum.Parse<FuelGrade>(s.FuelGrade, true)))
                .ForMember(d => d.TotalPrice, opt => opt.MapFrom(s => new Money(s.TotalPrice, ParseCurrency(s.Currency))))
                .ForMember(d => d.UnitPrice, opt => opt.MapFrom(s => new Money(s.UnitPrice, ParseCurrency(s.Currency))))
                .ForMember(d => d.Station, opt => opt.Ignore())
                .ForMember(d => d.Discounts, opt => opt.Ignore())
                .ForMember(d => d.CreateDate, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<ApiGasLogForUpdate, GasLog>()
                .ForMember(d => d.Odometer, opt =>
                {
                    opt.PreCondition(s => s.Odometer.HasValue);
                    opt.MapFrom(s => new Dimension((double)s.Odometer.Value, ParseDimensionUnit(s.OdometerUnit), 3));
                })
                .ForMember(d => d.Distance, opt =>
                {
                    opt.PreCondition(s => s.Distance.HasValue);
                    opt.MapFrom(s => new Dimension((double)s.Distance.Value, ParseDimensionUnit(s.DistanceUnit), 3));
                })
                .ForMember(d => d.Fuel, opt =>
                {
                    opt.PreCondition(s => s.Fuel.HasValue);
                    opt.MapFrom(s => new Volume((double)s.Fuel.Value, ParseVolumeUnit(s.FuelUnit), 3));
                })
                .ForMember(d => d.FuelGrade, opt =>
                {
                    opt.PreCondition(s => s.FuelGrade != null);
                    opt.MapFrom(s => Enum.Parse<FuelGrade>(s.FuelGrade, true));
                })
                .ForMember(d => d.TotalPrice, opt =>
                {
                    opt.PreCondition(s => s.TotalPrice.HasValue);
                    opt.MapFrom(s => new Money(s.TotalPrice.Value, ParseCurrency(s.Currency)));
                })
                .ForMember(d => d.UnitPrice, opt =>
                {
                    opt.PreCondition(s => s.UnitPrice.HasValue);
                    opt.MapFrom(s => new Money(s.UnitPrice.Value, ParseCurrency(s.Currency)));
                })
                .ForMember(d => d.Station, opt => opt.Ignore())
                .ForMember(d => d.Discounts, opt => opt.Ignore())
                .ForMember(d => d.LastModifiedDate, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<GasLog, ApiGasLogForUpdate>()
                .ForMember(d => d.Odometer, opt => opt.MapFrom(s => (decimal)s.Odometer.Value))
                .ForMember(d => d.OdometerUnit, opt => opt.MapFrom(s => s.Odometer.Unit.ToString()))
                .ForMember(d => d.Distance, opt => opt.MapFrom(s => (decimal)s.Distance.Value))
                .ForMember(d => d.DistanceUnit, opt => opt.MapFrom(s => s.Distance.Unit.ToString()))
                .ForMember(d => d.Fuel, opt => opt.MapFrom(s => (decimal)s.Fuel.Value))
                .ForMember(d => d.FuelUnit, opt => opt.MapFrom(s => s.Fuel.Unit.ToString()))
                .ForMember(d => d.FuelGrade, opt => opt.MapFrom(s => s.FuelGrade.ToString()))
                .ForMember(d => d.TotalPrice, opt => opt.MapFrom(s => s.TotalPrice.Amount))
                .ForMember(d => d.UnitPrice, opt => opt.MapFrom(s => s.UnitPrice.Amount))
                .ForMember(d => d.Currency, opt => opt.MapFrom(s => s.TotalPrice.Currency.ToString()));

            CreateMap<PageList<GasLog>, PageList<ApiGasLog>>()
                .ConvertUsing(new PageListConverter<GasLog, ApiGasLog>());

            // GasDiscount mappings
            CreateMap<GasDiscount, ApiDiscount>()
                .ForMember(d => d.Amount, opt => opt.MapFrom(s => s.Amount.Amount))
                .ForMember(d => d.Currency, opt => opt.MapFrom(s => s.Amount.Currency.ToString()))
                .ForMember(d => d.DiscountType, opt => opt.MapFrom(s => s.DiscountType.ToString()));

            CreateMap<ApiDiscountForCreate, GasDiscount>()
                .ForMember(d => d.Amount, opt => opt.MapFrom(s => new Money(s.Amount, ParseCurrency(s.Currency))))
                .ForMember(d => d.DiscountType, opt => opt.MapFrom(s => Enum.Parse<GasDiscountType>(s.DiscountType, true)));

            CreateMap<ApiDiscountForUpdate, GasDiscount>()
                .ForMember(d => d.Amount, opt => opt.MapFrom(s => s.Amount.HasValue ? new Money(s.Amount.Value, ParseCurrency(s.Currency)) : default))
                .ForMember(d => d.DiscountType, opt => opt.MapFrom(s =>
                    string.IsNullOrEmpty(s.DiscountType) ? default : Enum.Parse<GasDiscountType>(s.DiscountType, true)))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<GasDiscount, ApiDiscountForUpdate>()
                .ForMember(d => d.Amount, opt => opt.MapFrom(s => s.Amount.Amount))
                .ForMember(d => d.Currency, opt => opt.MapFrom(s => s.Amount.Currency.ToString()))
                .ForMember(d => d.DiscountType, opt => opt.MapFrom(s => s.DiscountType.ToString()));

            CreateMap<PageList<GasDiscount>, PageList<ApiDiscount>>()
                .ConvertUsing(new PageListConverter<GasDiscount, ApiDiscount>());

            // GasDiscountInfo mappings
            CreateMap<GasDiscountInfo, ApiDiscountInfo>()
                .ForMember(d => d.DiscountId, opt => opt.MapFrom(s => s.Program.Id))
                .ForMember(d => d.Amount, opt => opt.MapFrom(s => s.Amount.Amount));

            // GasStation mappings
            CreateMap<GasStation, ApiGasStation>();

            CreateMap<ApiGasStationForCreate, GasStation>();

            CreateMap<ApiGasStationForUpdate, GasStation>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<GasStation, ApiGasStationForUpdate>();

            CreateMap<PageList<GasStation>, PageList<ApiGasStation>>()
                .ConvertUsing(new PageListConverter<GasStation, ApiGasStation>());

            // GeoAddress mappings
            CreateMap<GeoAddress, ApiGeoAddress>();
        }
    }
}
