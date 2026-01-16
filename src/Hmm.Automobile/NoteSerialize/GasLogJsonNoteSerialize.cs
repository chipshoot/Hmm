using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hmm.Automobile.DomainEntity;
using Hmm.Core.Map.DomainEntity;

namespace Hmm.Automobile.NoteSerialize
{
    /// <summary>
    /// JSON serializer for GasLog entities.
    /// Handles serialization of GasLog domain objects to/from JSON stored in HmmNote.Content.
    /// </summary>
    public class GasLogJsonNoteSerialize : EntityJsonNoteSerializeBase<GasLog>
    {
        private readonly IApplication _app;
        private readonly IAutoEntityManager<AutomobileInfo> _autoManager;
        private readonly IAutoEntityManager<GasDiscount> _discountManager;
        private readonly IAutoEntityManager<GasStation> _stationManager;
        private readonly IEntityLookup _lookupRepo;
        private readonly GasStationJsonSerializer _stationSerializer;

        public GasLogJsonNoteSerialize(
            IApplication app,
            ILogger<GasLog> logger,
            IAutoEntityManager<AutomobileInfo> autoManager,
            IAutoEntityManager<GasDiscount> discountManager,
            IAutoEntityManager<GasStation> stationManager,
            IEntityLookup lookupRepo)
            : base(logger)
        {
            ArgumentNullException.ThrowIfNull(app);
            ArgumentNullException.ThrowIfNull(autoManager);
            ArgumentNullException.ThrowIfNull(discountManager);
            ArgumentNullException.ThrowIfNull(stationManager);
            ArgumentNullException.ThrowIfNull(lookupRepo);

            _app = app;
            _autoManager = autoManager;
            _discountManager = discountManager;
            _stationManager = stationManager;
            _lookupRepo = lookupRepo;

            // Initialize station serializer
            _stationSerializer = new GasStationJsonSerializer(stationManager, logger, JsonOptions);
        }

        public override async Task<ProcessingResult<GasLog>> GetEntity(HmmNote note)
        {
            try
            {
                var (gasLogElement, document, error) = GetEntityRoot(note, AutomobileConstant.GasLogRecordSubject);
                if (!gasLogElement.HasValue || document == null)
                {
                    return ProcessingResult<GasLog>.Fail(
                        error ?? "Failed to parse gas log from note",
                        ErrorCategory.MappingError);
                }

                var gasLogJson = gasLogElement.Value;

                // Parse and resolve automobile
                var carId = GetIntProperty(gasLogJson, "automobile");
                var carResult = await _autoManager.GetEntityByIdAsync(carId);
                if (!carResult.Success || carResult.Value == null)
                {
                    document.Dispose();
                    return ProcessingResult<GasLog>.NotFound(
                        $"Cannot find automobile with ID: {carId}");
                }

                // Create GasLog entity
                var gasLog = new GasLog
                {
                    Id = note.Id,
                    Date = GetDateTimeProperty(gasLogJson, "date"),
                    Car = carResult.Value,
                    AutomobileId = carId,
                    Comment = GetStringProperty(gasLogJson, "comment", string.Empty),
                    CreateDate = GetDateTimeProperty(gasLogJson, "createDate"),
                    AuthorId = note.Author.Id
                };

                // Parse and resolve GasStation using dedicated serializer
                if (gasLogJson.TryGetProperty("station", out var stationElement))
                {
                    var stationResult = await _stationSerializer.DeserializeAsync(stationElement);
                    if (stationResult.Success && stationResult.Value != null)
                    {
                        gasLog.Station = stationResult.Value;
                    }
                    else if (stationResult.HasWarning)
                    {
                        Logger.LogWarning("Station deserialization warning: {Message}", stationResult.ErrorMessage);
                    }
                }

                // Parse Distance (Dimension)
                if (gasLogJson.TryGetProperty("distance", out var distanceElement))
                {
                    gasLog.Distance = JsonSerializer.Deserialize<Hmm.Utility.MeasureUnit.Dimension>(
                        distanceElement.GetRawText(), JsonOptions);
                }

                // Parse Odometer (Dimension)
                if (gasLogJson.TryGetProperty("odometer", out var odometerElement))
                {
                    gasLog.Odometer = JsonSerializer.Deserialize<Hmm.Utility.MeasureUnit.Dimension>(
                        odometerElement.GetRawText(), JsonOptions);
                }

                // Parse Fuel (Volume)
                if (gasLogJson.TryGetProperty("fuel", out var fuelElement))
                {
                    gasLog.Fuel = JsonSerializer.Deserialize<Hmm.Utility.MeasureUnit.Volume>(
                        fuelElement.GetRawText(), JsonOptions);
                }

                // Parse TotalPrice (Money)
                if (gasLogJson.TryGetProperty("totalPrice", out var priceElement))
                {
                    gasLog.TotalPrice = JsonSerializer.Deserialize<Hmm.Utility.Currency.Money>(
                        priceElement.GetRawText(), JsonOptions);
                }

                // Parse UnitPrice (Money) - optional
                if (gasLogJson.TryGetProperty("unitPrice", out var unitPriceElement))
                {
                    gasLog.UnitPrice = JsonSerializer.Deserialize<Hmm.Utility.Currency.Money>(
                        unitPriceElement.GetRawText(), JsonOptions);
                }

                // Parse FuelGrade - optional
                if (gasLogJson.TryGetProperty("fuelGrade", out var fuelGradeElement))
                {
                    if (Enum.TryParse<FuelGrade>(fuelGradeElement.GetString(), true, out var fuelGrade))
                    {
                        gasLog.FuelGrade = fuelGrade;
                    }
                }

                // Parse boolean flags
                if (gasLogJson.TryGetProperty("isFullTank", out var isFullTankElement))
                {
                    gasLog.IsFullTank = isFullTankElement.GetBoolean();
                }

                if (gasLogJson.TryGetProperty("isFirstFillUp", out var isFirstFillUpElement))
                {
                    gasLog.IsFirstFillUp = isFirstFillUpElement.GetBoolean();
                }

                // Parse driving context
                if (gasLogJson.TryGetProperty("cityDrivingPercentage", out var cityElement))
                {
                    gasLog.CityDrivingPercentage = cityElement.GetInt32();
                }

                if (gasLogJson.TryGetProperty("highwayDrivingPercentage", out var highwayElement))
                {
                    gasLog.HighwayDrivingPercentage = highwayElement.GetInt32();
                }

                // Parse receipt number
                if (gasLogJson.TryGetProperty("receiptNumber", out var receiptElement))
                {
                    gasLog.ReceiptNumber = receiptElement.GetString();
                }

                // Parse location
                if (gasLogJson.TryGetProperty("location", out var locationElement))
                {
                    gasLog.Location = locationElement.GetString();
                }

                // Parse Discounts array
                if (gasLogJson.TryGetProperty("discounts", out var discountsElement) &&
                    discountsElement.ValueKind == JsonValueKind.Array)
                {
                    var discounts = await GetDiscountInfosAsync(discountsElement);
                    if (discounts.Any())
                    {
                        gasLog.Discounts = discounts;
                    }
                }

                document.Dispose();
                return ProcessingResult<GasLog>.Ok(gasLog);
            }
            catch (JsonException ex)
            {
                Logger.LogError(ex, "JSON parsing error while deserializing GasLog");
                return ProcessingResult<GasLog>.Fail(
                    $"Invalid JSON format: {ex.Message}",
                    ErrorCategory.MappingError);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deserializing GasLog from note");
                return ProcessingResult<GasLog>.FromException(ex);
            }
        }

        public override string GetNoteSerializationText(GasLog entity)
        {
            if (entity == null)
            {
                return string.Empty;
            }

            try
            {
                // Build the gasLog JSON object
                var gasLogData = new Dictionary<string, object>
                {
                    ["date"] = entity.Date.ToString("o"), // ISO 8601 format
                    ["automobile"] = entity.AutomobileId,
                    ["distance"] = entity.Distance,
                    ["odometer"] = entity.Odometer,
                    ["fuel"] = entity.Fuel,
                    ["totalPrice"] = entity.TotalPrice,
                    ["fuelGrade"] = entity.FuelGrade.ToString(),
                    ["isFullTank"] = entity.IsFullTank,
                    ["isFirstFillUp"] = entity.IsFirstFillUp,
                    ["comment"] = entity.Comment ?? string.Empty,
                    ["createDate"] = entity.CreateDate.ToString("o")
                };

                // Serialize Station using dedicated serializer
                if (entity.Station != null)
                {
                    var stationResult = _stationSerializer.Serialize(entity.Station);
                    if (stationResult.Success && stationResult.Value != null)
                    {
                        gasLogData["station"] = stationResult.Value;
                    }
                    else
                    {
                        Logger.LogWarning("Failed to serialize station: {Error}", stationResult.ErrorMessage);
                    }
                }

                // Add UnitPrice if available
                if (entity.UnitPrice != null && entity.UnitPrice.InternalAmount > 0)
                {
                    gasLogData["unitPrice"] = entity.UnitPrice;
                }

                // Add driving context if available
                if (entity.CityDrivingPercentage.HasValue)
                {
                    gasLogData["cityDrivingPercentage"] = entity.CityDrivingPercentage.Value;
                }

                if (entity.HighwayDrivingPercentage.HasValue)
                {
                    gasLogData["highwayDrivingPercentage"] = entity.HighwayDrivingPercentage.Value;
                }

                // Add receipt number if available
                if (!string.IsNullOrEmpty(entity.ReceiptNumber))
                {
                    gasLogData["receiptNumber"] = entity.ReceiptNumber;
                }

                // Add location if available
                if (!string.IsNullOrEmpty(entity.Location))
                {
                    gasLogData["location"] = entity.Location;
                }

                // Add discounts array
                var discountsList = new List<object>();
                if (entity.Discounts != null && entity.Discounts.Any())
                {
                    foreach (var discount in entity.Discounts)
                    {
                        if (discount.Amount == null || discount.Program == null)
                        {
                            continue;
                        }

                        discountsList.Add(new
                        {
                            amount = discount.Amount,
                            programId = discount.Program.Id
                        });
                    }
                }
                gasLogData["discounts"] = discountsList;

                // Create the full note structure
                var noteStructure = new
                {
                    note = new
                    {
                        content = new Dictionary<string, object>
                        {
                            [AutomobileConstant.GasLogRecordSubject] = gasLogData
                        }
                    }
                };

                // Serialize to JSON
                var json = JsonSerializer.Serialize(noteStructure, JsonOptions);
                return json;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error serializing GasLog to JSON");
                return string.Empty;
            }
        }

        /// <summary>
        /// Parses discount information from JSON array.
        /// </summary>
        /// <param name="discountsElement">The JSON array containing discount objects.</param>
        /// <returns>List of GasDiscountInfo objects.</returns>
        private async Task<List<GasDiscountInfo>> GetDiscountInfosAsync(JsonElement discountsElement)
        {
            var infos = new List<GasDiscountInfo>();

            if (discountsElement.ValueKind != JsonValueKind.Array)
            {
                return infos;
            }

            foreach (var discountElement in discountsElement.EnumerateArray())
            {
                try
                {
                    // Parse amount (Money)
                    if (!discountElement.TryGetProperty("amount", out var amountElement))
                    {
                        continue;
                    }

                    var money = JsonSerializer.Deserialize<Hmm.Utility.Currency.Money>(
                        amountElement.GetRawText(), JsonOptions);

                    // Parse program ID
                    if (!discountElement.TryGetProperty("programId", out var programIdElement) ||
                        !programIdElement.TryGetInt32(out var discountId))
                    {
                        continue;
                    }

                    // Resolve discount program entity
                    var discountResult = await _discountManager.GetEntityByIdAsync(discountId);
                    if (!discountResult.Success || discountResult.Value == null)
                    {
                        Logger.LogWarning("Cannot find discount program with ID: {DiscountId}", discountId);
                        continue;
                    }

                    infos.Add(new GasDiscountInfo
                    {
                        Amount = money,
                        Program = discountResult.Value
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error parsing discount info");
                }
            }

            return infos;
        }

        protected override NoteCatalog GetCatalog()
        {
            return _app.GetCatalog(NoteCatalogType.GasLog, _lookupRepo);
        }
    }
}
