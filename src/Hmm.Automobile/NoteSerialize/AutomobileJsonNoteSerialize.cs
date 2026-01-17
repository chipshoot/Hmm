using Hmm.Automobile.DomainEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hmm.Automobile.NoteSerialize
{
    /// <summary>
    /// JSON serializer for AutomobileInfo entities.
    /// Handles serialization of AutomobileInfo domain objects to/from JSON stored in HmmNote.Content.
    /// </summary>
    public class AutomobileJsonNoteSerialize : EntityJsonNoteSerializeBase<AutomobileInfo>
    {
        private readonly IApplication _app;
        private readonly IEntityLookup _lookupRepo;

        public AutomobileJsonNoteSerialize(
            IApplication app,
            ILogger<AutomobileInfo> logger,
            IEntityLookup lookupRepo)
            : base(logger)
        {
            ArgumentNullException.ThrowIfNull(app);
            ArgumentNullException.ThrowIfNull(lookupRepo);

            _app = app;
            _lookupRepo = lookupRepo;
        }

        public override Task<ProcessingResult<AutomobileInfo>> GetEntity(HmmNote note)
        {
            try
            {
                var (automobileElement, document, error) = GetEntityRoot(note, AutomobileConstant.AutoMobileRecordSubject);
                if (!automobileElement.HasValue || document == null)
                {
                    return Task.FromResult(ProcessingResult<AutomobileInfo>.Fail(
                        error ?? "Failed to parse automobile from note",
                        ErrorCategory.MappingError));
                }

                var autoJson = automobileElement.Value;

                // Parse enums with defaults
                Enum.TryParse<FuelEngineType>(GetStringProperty(autoJson, "engineType"), out var engineType);
                Enum.TryParse<FuelGrade>(GetStringProperty(autoJson, "fuelType"), out var fuelType);
                Enum.TryParse<OwnershipType>(GetStringProperty(autoJson, "ownershipStatus"), out var ownershipStatus);

                var automobile = new AutomobileInfo
                {
                    Id = note.Id,
                    AuthorId = note.Author.Id,

                    // Core Identification
                    VIN = GetStringProperty(autoJson, "vin"),
                    Maker = GetStringProperty(autoJson, "maker"),
                    Brand = GetStringProperty(autoJson, "brand"),
                    Model = GetStringProperty(autoJson, "model"),
                    Trim = GetStringProperty(autoJson, "trim"),
                    Year = GetIntProperty(autoJson, "year"),
                    Color = GetStringProperty(autoJson, "color"),
                    Plate = GetStringProperty(autoJson, "plate"),

                    // Fuel & Engine
                    EngineType = engineType,
                    FuelType = fuelType,
                    FuelTankCapacity = GetDecimalProperty(autoJson, "fuelTankCapacity"),
                    CityMPG = GetDecimalProperty(autoJson, "cityMPG"),
                    HighwayMPG = GetDecimalProperty(autoJson, "highwayMPG"),
                    CombinedMPG = GetDecimalProperty(autoJson, "combinedMPG"),

                    // Meter/Odometer
                    MeterReading = GetLongProperty(autoJson, "meterReading"),
                    PurchaseMeterReading = GetNullableIntProperty(autoJson, "purchaseMeterReading"),

                    // Ownership
                    PurchaseDate = GetNullableDateTimeProperty(autoJson, "purchaseDate"),
                    PurchasePrice = GetNullableDecimalProperty(autoJson, "purchasePrice"),
                    OwnershipStatus = ownershipStatus,

                    // Status
                    IsActive = GetBoolProperty(autoJson, "isActive", true),
                    SoldDate = GetNullableDateTimeProperty(autoJson, "soldDate"),
                    SoldMeterReading = GetNullableIntProperty(autoJson, "soldMeterReading"),
                    SoldPrice = GetNullableDecimalProperty(autoJson, "soldPrice"),

                    // Registration & Insurance
                    RegistrationExpiryDate = GetNullableDateTimeProperty(autoJson, "registrationExpiryDate"),
                    InsuranceExpiryDate = GetNullableDateTimeProperty(autoJson, "insuranceExpiryDate"),
                    InsuranceProvider = GetStringProperty(autoJson, "insuranceProvider"),
                    InsurancePolicyNumber = GetStringProperty(autoJson, "insurancePolicyNumber"),

                    // Maintenance
                    LastServiceDate = GetNullableDateTimeProperty(autoJson, "lastServiceDate"),
                    LastServiceMeterReading = GetNullableIntProperty(autoJson, "lastServiceMeterReading"),
                    NextServiceDueDate = GetNullableDateTimeProperty(autoJson, "nextServiceDueDate"),
                    NextServiceDueMeterReading = GetNullableIntProperty(autoJson, "nextServiceDueMeterReading"),

                    // Metadata
                    Notes = GetStringProperty(autoJson, "notes"),
                    CreatedDate = GetDateTimeProperty(autoJson, "createdDate"),
                    LastModifiedDate = GetDateTimeProperty(autoJson, "lastModifiedDate")
                };

                document.Dispose();
                return Task.FromResult(ProcessingResult<AutomobileInfo>.Ok(automobile));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult<AutomobileInfo>.FromException(ex));
            }
        }

        public override string GetNoteSerializationText(AutomobileInfo entity)
        {
            if (entity == null)
            {
                return string.Empty;
            }

            try
            {
                var autoData = new Dictionary<string, object>
                {
                    // Core Identification
                    ["vin"] = entity.VIN ?? string.Empty,
                    ["maker"] = entity.Maker ?? string.Empty,
                    ["brand"] = entity.Brand ?? string.Empty,
                    ["model"] = entity.Model ?? string.Empty,
                    ["trim"] = entity.Trim ?? string.Empty,
                    ["year"] = entity.Year,
                    ["color"] = entity.Color ?? string.Empty,
                    ["plate"] = entity.Plate ?? string.Empty,

                    // Fuel & Engine
                    ["engineType"] = entity.EngineType.ToString(),
                    ["fuelType"] = entity.FuelType.ToString(),
                    ["fuelTankCapacity"] = entity.FuelTankCapacity,
                    ["cityMPG"] = entity.CityMPG,
                    ["highwayMPG"] = entity.HighwayMPG,
                    ["combinedMPG"] = entity.CombinedMPG,

                    // Meter/Odometer
                    ["meterReading"] = entity.MeterReading,

                    // Ownership
                    ["ownershipStatus"] = entity.OwnershipStatus.ToString(),

                    // Status
                    ["isActive"] = entity.IsActive,

                    // Metadata
                    ["notes"] = entity.Notes ?? string.Empty,
                    ["createdDate"] = entity.CreatedDate.ToString("o"),
                    ["lastModifiedDate"] = entity.LastModifiedDate.ToString("o")
                };

                // Add optional nullable properties only if they have values
                if (entity.PurchaseMeterReading.HasValue)
                    autoData["purchaseMeterReading"] = entity.PurchaseMeterReading.Value;

                if (entity.PurchaseDate.HasValue)
                    autoData["purchaseDate"] = entity.PurchaseDate.Value.ToString("o");

                if (entity.PurchasePrice.HasValue)
                    autoData["purchasePrice"] = entity.PurchasePrice.Value;

                if (entity.SoldDate.HasValue)
                    autoData["soldDate"] = entity.SoldDate.Value.ToString("o");

                if (entity.SoldMeterReading.HasValue)
                    autoData["soldMeterReading"] = entity.SoldMeterReading.Value;

                if (entity.SoldPrice.HasValue)
                    autoData["soldPrice"] = entity.SoldPrice.Value;

                if (entity.RegistrationExpiryDate.HasValue)
                    autoData["registrationExpiryDate"] = entity.RegistrationExpiryDate.Value.ToString("o");

                if (entity.InsuranceExpiryDate.HasValue)
                    autoData["insuranceExpiryDate"] = entity.InsuranceExpiryDate.Value.ToString("o");

                if (!string.IsNullOrEmpty(entity.InsuranceProvider))
                    autoData["insuranceProvider"] = entity.InsuranceProvider;

                if (!string.IsNullOrEmpty(entity.InsurancePolicyNumber))
                    autoData["insurancePolicyNumber"] = entity.InsurancePolicyNumber;

                if (entity.LastServiceDate.HasValue)
                    autoData["lastServiceDate"] = entity.LastServiceDate.Value.ToString("o");

                if (entity.LastServiceMeterReading.HasValue)
                    autoData["lastServiceMeterReading"] = entity.LastServiceMeterReading.Value;

                if (entity.NextServiceDueDate.HasValue)
                    autoData["nextServiceDueDate"] = entity.NextServiceDueDate.Value.ToString("o");

                if (entity.NextServiceDueMeterReading.HasValue)
                    autoData["nextServiceDueMeterReading"] = entity.NextServiceDueMeterReading.Value;

                // Create the full note structure
                var noteStructure = new
                {
                    note = new
                    {
                        content = new Dictionary<string, object>
                        {
                            [AutomobileConstant.AutoMobileRecordSubject] = autoData
                        }
                    }
                };

                return JsonSerializer.Serialize(noteStructure, JsonOptions);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error serializing AutomobileInfo to JSON");
                return string.Empty;
            }
        }

        protected override NoteCatalog GetCatalog()
        {
            return _app.GetCatalog(NoteCatalogType.Automobile, _lookupRepo);
        }

        #region Helper Methods for Nullable Types

        private decimal GetDecimalProperty(JsonElement element, string propertyName, decimal defaultValue = 0)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.TryGetDecimal(out var value))
            {
                return value;
            }
            return defaultValue;
        }

        private long GetLongProperty(JsonElement element, string propertyName, long defaultValue = 0)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.TryGetInt64(out var value))
            {
                return value;
            }
            return defaultValue;
        }

        // Note: GetBoolProperty is inherited from DefaultJsonNoteSerializer<T>

        private int? GetNullableIntProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind != JsonValueKind.Null &&
                property.TryGetInt32(out var value))
            {
                return value;
            }
            return null;
        }

        private decimal? GetNullableDecimalProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind != JsonValueKind.Null &&
                property.TryGetDecimal(out var value))
            {
                return value;
            }
            return null;
        }

        private DateTime? GetNullableDateTimeProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind != JsonValueKind.Null &&
                property.TryGetDateTime(out var value))
            {
                return value;
            }
            return null;
        }

        #endregion
    }
}
