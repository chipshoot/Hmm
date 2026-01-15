using Hmm.Utility.Dal.Query;
using Hmm.Utility.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        private readonly IEntityLookup _lookupRepo;

        public GasLogJsonNoteSerialize(
            IApplication app,
            ILogger<GasLog> logger,
            IAutoEntityManager<AutomobileInfo> autoManager,
            IAutoEntityManager<GasDiscount> discountManager,
            IEntityLookup lookupRepo)
            : base(logger)
        {
            ArgumentNullException.ThrowIfNull(app);
            ArgumentNullException.ThrowIfNull(autoManager);
            ArgumentNullException.ThrowIfNull(discountManager);
            ArgumentNullException.ThrowIfNull(lookupRepo);

            _app = app;
            _autoManager = autoManager;
            _discountManager = discountManager;
            _lookupRepo = lookupRepo;
        }

        public override GasLog GetEntity(HmmNote note)
        {
            try
            {
                var (gasLogElement, document) = GetEntityRoot(note, AutomobileConstant.GasLogRecordSubject);
                if (!gasLogElement.HasValue || document == null)
                {
                    return null;
                }

                var gasLogJson = gasLogElement.Value;

                // Parse automobile ID and resolve entity
                var carId = GetIntProperty(gasLogJson, "automobile");
                var car = _autoManager.GetEntityById(carId);
                if (car == null)
                {
                    ProcessResult.AddErrorMessage($"Cannot find automobile with ID: {carId}");
                    return null;
                }

                // Create GasLog entity
                var gasLog = new GasLog
                {
                    Id = note.Id,
                    Date = GetDateTimeProperty(gasLogJson, "date"),
                    Car = car,
                    AutomobileId = carId,
                    Station = GetStringProperty(gasLogJson, "station", string.Empty),
                    Comment = GetStringProperty(gasLogJson, "comment", string.Empty),
                    CreateDate = GetDateTimeProperty(gasLogJson, "createDate"),
                    AuthorId = note.Author.Id
                };

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

                // Parse Discounts array
                if (gasLogJson.TryGetProperty("discounts", out var discountsElement) &&
                    discountsElement.ValueKind == JsonValueKind.Array)
                {
                    var discounts = GetDiscountInfos(discountsElement);
                    if (discounts.Any())
                    {
                        gasLog.Discounts = discounts;
                    }
                }

                document.Dispose();
                return gasLog;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                throw;
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
                    ["automobile"] = entity.Car.Id,
                    ["station"] = entity.Station ?? string.Empty,
                    ["distance"] = entity.Distance,
                    ["odometer"] = entity.Odometer,
                    ["fuel"] = entity.Fuel,
                    ["totalPrice"] = entity.TotalPrice,
                    ["comment"] = entity.Comment ?? string.Empty,
                    ["createDate"] = entity.CreateDate.ToString("o")
                };

                // Add UnitPrice if available
                if (entity.UnitPrice != null && entity.UnitPrice.InternalAmount > 0)
                {
                    gasLogData["unitPrice"] = entity.UnitPrice;
                }

                // Add discounts array
                var discountsList = new List<object>();
                if (entity.Discounts != null && entity.Discounts.Any())
                {
                    foreach (var discount in entity.Discounts)
                    {
                        if (discount.Amount == null || discount.Program == null)
                        {
                            ProcessResult.AddErrorMessage("Cannot found valid discount information, amount or discount program is missing");
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

                // Serialize to JSON without indentation (compact format)
                var json = JsonSerializer.Serialize(noteStructure, JsonOptions);
                return json;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Parses discount information from JSON array.
        /// </summary>
        /// <param name="discountsElement">The JSON array containing discount objects.</param>
        /// <returns>List of GasDiscountInfo objects.</returns>
        private List<GasDiscountInfo> GetDiscountInfos(JsonElement discountsElement)
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
                        ProcessResult.AddErrorMessage("Cannot find money information from discount JSON");
                        continue;
                    }

                    var money = JsonSerializer.Deserialize<Hmm.Utility.Currency.Money>(
                        amountElement.GetRawText(), JsonOptions);

                    // Parse program ID
                    if (!discountElement.TryGetProperty("programId", out var programIdElement) ||
                        !programIdElement.TryGetInt32(out var discountId))
                    {
                        ProcessResult.AddErrorMessage("Cannot find valid discount program ID from JSON");
                        continue;
                    }

                    // Resolve discount program entity
                    var discount = _discountManager.GetEntityById(discountId);
                    if (discount == null)
                    {
                        ProcessResult.AddErrorMessage($"Cannot find discount program with ID: {discountId} from data source");
                        continue;
                    }

                    infos.Add(new GasDiscountInfo
                    {
                        Amount = money,
                        Program = discount
                    });
                }
                catch (Exception ex)
                {
                    ProcessResult.AddErrorMessage($"Error parsing discount: {ex.Message}");
                }
            }

            return infos;
        }

        protected override DomainEntity.NoteCatalog GetCatalog()
        {
            return _app.GetCatalog(NoteCatalogType.GasLog, _lookupRepo);
        }
    }
}
