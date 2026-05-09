using Hmm.Automobile.DomainEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Currency;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hmm.Automobile.NoteSerialize
{
    /// <summary>
    /// JSON serializer for ServiceRecord. Round-trips the record to/from HmmNote.Content
    /// under the ServiceRecord catalog and subject prefix.
    /// </summary>
    public class ServiceRecordJsonNoteSerialize : EntityJsonNoteSerializeBase<ServiceRecord>
    {
        private readonly INoteCatalogProvider _catalogProvider;

        public ServiceRecordJsonNoteSerialize(
            INoteCatalogProvider catalogProvider,
            ILogger<ServiceRecord> logger)
            : base(logger)
        {
            ArgumentNullException.ThrowIfNull(catalogProvider);
            _catalogProvider = catalogProvider;
        }

        public override Task<ProcessingResult<ServiceRecord>> GetEntity(HmmNote note)
        {
            try
            {
                var (recordElement, document, error) = GetEntityRoot(note, AutomobileConstant.ServiceRecordSubject);
                if (!recordElement.HasValue || document == null)
                {
                    return Task.FromResult(ProcessingResult<ServiceRecord>.Fail(
                        error ?? "Failed to parse service record from note",
                        ErrorCategory.MappingError));
                }

                var recordJson = recordElement.Value;

                Money cost = null;
                if (recordJson.TryGetProperty("cost", out var costElement) &&
                    costElement.ValueKind != JsonValueKind.Null)
                {
                    cost = JsonSerializer.Deserialize<Money>(costElement.GetRawText(), JsonOptions);
                }

                Enum.TryParse<ServiceType>(GetStringProperty(recordJson, "type"), true, out var type);

                var parts = new List<PartItem>();
                if (recordJson.TryGetProperty("parts", out var partsElement) &&
                    partsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var part in partsElement.EnumerateArray())
                    {
                        Money unit = null;
                        if (part.TryGetProperty("unitCost", out var unitElement) &&
                            unitElement.ValueKind != JsonValueKind.Null)
                        {
                            unit = JsonSerializer.Deserialize<Money>(unitElement.GetRawText(), JsonOptions);
                        }
                        parts.Add(new PartItem
                        {
                            Name = GetStringProperty(part, "name", string.Empty),
                            Quantity = GetIntProperty(part, "quantity", 1),
                            UnitCost = unit
                        });
                    }
                }

                var record = new ServiceRecord
                {
                    Id = note.Id,
                    AuthorId = note.Author.Id,
                    AutomobileId = GetIntProperty(recordJson, "automobileId"),
                    Date = GetDateTimeProperty(recordJson, "date"),
                    Mileage = GetIntProperty(recordJson, "mileage"),
                    Type = type,
                    Description = GetStringProperty(recordJson, "description", string.Empty),
                    Cost = cost,
                    ShopName = GetStringProperty(recordJson, "shopName", string.Empty),
                    Parts = parts,
                    Notes = GetStringProperty(recordJson, "notes", string.Empty),
                    CreatedDate = GetDateTimeProperty(recordJson, "createdDate"),
                    IsDeleted = note.IsDeleted
                };

                document.Dispose();
                return Task.FromResult(ProcessingResult<ServiceRecord>.Ok(record));
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error deserializing ServiceRecord from note");
                return Task.FromResult(ProcessingResult<ServiceRecord>.FromException(ex));
            }
        }

        public override string GetNoteSerializationText(ServiceRecord entity)
        {
            if (entity == null)
            {
                return string.Empty;
            }

            try
            {
                var partsList = new List<object>();
                if (entity.Parts != null)
                {
                    foreach (var p in entity.Parts)
                    {
                        partsList.Add(new
                        {
                            name = p.Name ?? string.Empty,
                            quantity = p.Quantity,
                            unitCost = p.UnitCost
                        });
                    }
                }

                var recordData = new Dictionary<string, object>
                {
                    ["automobileId"] = entity.AutomobileId,
                    ["date"] = entity.Date.ToString("o"),
                    ["mileage"] = entity.Mileage,
                    ["type"] = entity.Type.ToString(),
                    ["description"] = entity.Description ?? string.Empty,
                    ["cost"] = entity.Cost,
                    ["shopName"] = entity.ShopName ?? string.Empty,
                    ["parts"] = partsList,
                    ["notes"] = entity.Notes ?? string.Empty,
                    ["createdDate"] = entity.CreatedDate.ToString("o")
                };

                var noteStructure = new
                {
                    note = new
                    {
                        content = new Dictionary<string, object>
                        {
                            [AutomobileConstant.ServiceRecordSubject] = recordData
                        }
                    }
                };

                return JsonSerializer.Serialize(noteStructure, JsonOptions);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error serializing ServiceRecord to JSON");
                return string.Empty;
            }
        }

        protected override Task<NoteCatalog> GetCatalogAsync()
        {
            return _catalogProvider.GetCatalogAsync(NoteCatalogType.ServiceRecord);
        }
    }
}
