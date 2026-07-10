using Hmm.Automobile.DomainEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Currency;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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

                Money tax = null;
                if (recordJson.TryGetProperty("tax", out var taxElement) &&
                    taxElement.ValueKind != JsonValueKind.Null)
                {
                    tax = JsonSerializer.Deserialize<Money>(taxElement.GetRawText(), JsonOptions);
                }

                var types = new List<ServiceType>();
                if (recordJson.TryGetProperty("types", out var typesElement) &&
                    typesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var t in typesElement.EnumerateArray())
                    {
                        if (t.ValueKind == JsonValueKind.String &&
                            Enum.TryParse<ServiceType>(t.GetString(), true, out var parsed))
                        {
                            types.Add(parsed);
                        }
                    }
                }
                if (types.Count == 0)
                {
                    // Legacy payload: a single scalar "type".
                    Enum.TryParse<ServiceType>(GetStringProperty(recordJson, "type"), true, out var legacyType);
                    types.Add(legacyType);
                }

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
                        LineItemType itemType = LineItemType.Part;
                        if (part.TryGetProperty("type", out var typeEl) &&
                            typeEl.ValueKind == JsonValueKind.String)
                        {
                            Enum.TryParse(typeEl.GetString(), true, out itemType);
                        }
                        parts.Add(new PartItem
                        {
                            Type = itemType,
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
                    Types = types,
                    Name = GetStringProperty(recordJson, "name", string.Empty),
                    ReferenceNumber = GetStringProperty(recordJson, "referenceNumber", string.Empty),
                    Description = GetStringProperty(recordJson, "description", string.Empty),
                    Cost = cost,
                    Tax = tax,
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
                            type = p.Type.ToString(),
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
                    ["types"] = entity.Types.Select(t => t.ToString()).ToList(),
                    ["name"] = entity.Name ?? string.Empty,
                    ["referenceNumber"] = entity.ReferenceNumber ?? string.Empty,
                    ["description"] = entity.Description ?? string.Empty,
                    ["cost"] = entity.Cost,
                    ["tax"] = entity.Tax,
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
