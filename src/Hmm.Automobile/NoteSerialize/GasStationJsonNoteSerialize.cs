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
    /// JSON serializer for GasStation entities.
    /// Handles serialization of GasStation domain objects to/from JSON stored in HmmNote.Content.
    /// </summary>
    public class GasStationJsonNoteSerialize : EntityJsonNoteSerializeBase<GasStation>
    {
        private readonly IApplication _app;
        private readonly IEntityLookup _lookupRepo;

        public GasStationJsonNoteSerialize(
            IApplication app,
            ILogger<GasStation> logger,
            IEntityLookup lookupRepo)
            : base(logger)
        {
            ArgumentNullException.ThrowIfNull(app);
            ArgumentNullException.ThrowIfNull(lookupRepo);

            _app = app;
            _lookupRepo = lookupRepo;
        }

        public override Task<ProcessingResult<GasStation>> GetEntity(HmmNote note)
        {
            try
            {
                var (stationElement, document, error) = GetEntityRoot(note, AutomobileConstant.GasStationRecordSubject);
                if (!stationElement.HasValue || document == null)
                {
                    return Task.FromResult(ProcessingResult<GasStation>.Fail(
                        error ?? "Failed to parse gas station from note",
                        ErrorCategory.MappingError));
                }

                var stationJson = stationElement.Value;

                var station = new GasStation
                {
                    Id = note.Id,
                    AuthorId = note.Author.Id,
                    Name = GetStringProperty(stationJson, "name"),
                    Address = GetStringProperty(stationJson, "address"),
                    City = GetStringProperty(stationJson, "city"),
                    State = GetStringProperty(stationJson, "state"),
                    ZipCode = GetStringProperty(stationJson, "zipCode"),
                    Description = GetStringProperty(stationJson, "description"),
                    IsActive = GetBoolProperty(stationJson, "isActive", true)
                };

                document.Dispose();
                return Task.FromResult(ProcessingResult<GasStation>.Ok(station));
            }
            catch (JsonException ex)
            {
                Logger?.LogError(ex, "JSON parsing error while deserializing GasStation");
                return Task.FromResult(ProcessingResult<GasStation>.Fail(
                    $"Invalid JSON format: {ex.Message}",
                    ErrorCategory.MappingError));
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error deserializing GasStation from note");
                return Task.FromResult(ProcessingResult<GasStation>.FromException(ex));
            }
        }

        public override string GetNoteSerializationText(GasStation entity)
        {
            if (entity == null)
            {
                return string.Empty;
            }

            try
            {
                var stationData = new Dictionary<string, object>
                {
                    ["name"] = entity.Name ?? string.Empty,
                    ["address"] = entity.Address ?? string.Empty,
                    ["city"] = entity.City ?? string.Empty,
                    ["state"] = entity.State ?? string.Empty,
                    ["zipCode"] = entity.ZipCode ?? string.Empty,
                    ["description"] = entity.Description ?? string.Empty,
                    ["isActive"] = entity.IsActive
                };

                // Create the full note structure
                var noteStructure = new
                {
                    note = new
                    {
                        content = new Dictionary<string, object>
                        {
                            [AutomobileConstant.GasStationRecordSubject] = stationData
                        }
                    }
                };

                return JsonSerializer.Serialize(noteStructure, JsonOptions);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error serializing GasStation to JSON");
                return string.Empty;
            }
        }

        protected override Task<NoteCatalog> GetCatalogAsync()
        {
            return _app.GetCatalogAsync(NoteCatalogType.GasStation, _lookupRepo);
        }
    }
}
