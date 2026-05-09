using Hmm.Automobile.DomainEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hmm.Automobile.NoteSerialize
{
    /// <summary>
    /// JSON serializer for AutoScheduledService. Round-trips the schedule to/from
    /// HmmNote.Content under the AutoScheduledService catalog and subject prefix.
    /// </summary>
    public class AutoScheduledServiceJsonNoteSerialize : EntityJsonNoteSerializeBase<AutoScheduledService>
    {
        private readonly INoteCatalogProvider _catalogProvider;

        public AutoScheduledServiceJsonNoteSerialize(
            INoteCatalogProvider catalogProvider,
            ILogger<AutoScheduledService> logger)
            : base(logger)
        {
            ArgumentNullException.ThrowIfNull(catalogProvider);
            _catalogProvider = catalogProvider;
        }

        public override Task<ProcessingResult<AutoScheduledService>> GetEntity(HmmNote note)
        {
            try
            {
                var (scheduleElement, document, error) = GetEntityRoot(note, AutomobileConstant.AutoScheduledServiceSubject);
                if (!scheduleElement.HasValue || document == null)
                {
                    return Task.FromResult(ProcessingResult<AutoScheduledService>.Fail(
                        error ?? "Failed to parse scheduled service from note",
                        ErrorCategory.MappingError));
                }

                var scheduleJson = scheduleElement.Value;

                Enum.TryParse<ServiceType>(GetStringProperty(scheduleJson, "type"), true, out var type);

                int? intervalDays = null;
                if (scheduleJson.TryGetProperty("intervalDays", out var idElement) &&
                    idElement.ValueKind != JsonValueKind.Null)
                {
                    intervalDays = idElement.GetInt32();
                }

                int? intervalMileage = null;
                if (scheduleJson.TryGetProperty("intervalMileage", out var imElement) &&
                    imElement.ValueKind != JsonValueKind.Null)
                {
                    intervalMileage = imElement.GetInt32();
                }

                DateTime? nextDueDate = null;
                if (scheduleJson.TryGetProperty("nextDueDate", out var ndElement) &&
                    ndElement.ValueKind != JsonValueKind.Null)
                {
                    nextDueDate = ndElement.GetDateTime();
                }

                int? nextDueMileage = null;
                if (scheduleJson.TryGetProperty("nextDueMileage", out var nmElement) &&
                    nmElement.ValueKind != JsonValueKind.Null)
                {
                    nextDueMileage = nmElement.GetInt32();
                }

                var schedule = new AutoScheduledService
                {
                    Id = note.Id,
                    AuthorId = note.Author.Id,
                    AutomobileId = GetIntProperty(scheduleJson, "automobileId"),
                    Name = GetStringProperty(scheduleJson, "name", string.Empty),
                    Type = type,
                    IntervalDays = intervalDays,
                    IntervalMileage = intervalMileage,
                    NextDueDate = nextDueDate,
                    NextDueMileage = nextDueMileage,
                    IsActive = GetBoolProperty(scheduleJson, "isActive", true),
                    Notes = GetStringProperty(scheduleJson, "notes", string.Empty),
                    CreatedDate = GetDateTimeProperty(scheduleJson, "createdDate"),
                    LastModifiedDate = GetDateTimeProperty(scheduleJson, "lastModifiedDate"),
                    IsDeleted = note.IsDeleted
                };

                document.Dispose();
                return Task.FromResult(ProcessingResult<AutoScheduledService>.Ok(schedule));
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error deserializing AutoScheduledService from note");
                return Task.FromResult(ProcessingResult<AutoScheduledService>.FromException(ex));
            }
        }

        public override string GetNoteSerializationText(AutoScheduledService entity)
        {
            if (entity == null)
            {
                return string.Empty;
            }

            try
            {
                var scheduleData = new Dictionary<string, object>
                {
                    ["automobileId"] = entity.AutomobileId,
                    ["name"] = entity.Name ?? string.Empty,
                    ["type"] = entity.Type.ToString(),
                    ["intervalDays"] = entity.IntervalDays,
                    ["intervalMileage"] = entity.IntervalMileage,
                    ["nextDueDate"] = entity.NextDueDate?.ToString("o"),
                    ["nextDueMileage"] = entity.NextDueMileage,
                    ["isActive"] = entity.IsActive,
                    ["notes"] = entity.Notes ?? string.Empty,
                    ["createdDate"] = entity.CreatedDate.ToString("o"),
                    ["lastModifiedDate"] = entity.LastModifiedDate.ToString("o")
                };

                var noteStructure = new
                {
                    note = new
                    {
                        content = new Dictionary<string, object>
                        {
                            [AutomobileConstant.AutoScheduledServiceSubject] = scheduleData
                        }
                    }
                };

                return JsonSerializer.Serialize(noteStructure, JsonOptions);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error serializing AutoScheduledService to JSON");
                return string.Empty;
            }
        }

        protected override Task<NoteCatalog> GetCatalogAsync()
        {
            return _catalogProvider.GetCatalogAsync(NoteCatalogType.AutoScheduledService);
        }
    }
}
