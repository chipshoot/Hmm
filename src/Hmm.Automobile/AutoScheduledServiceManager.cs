using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    public class AutoScheduledServiceManager : EntityManagerBase<AutoScheduledService>, IAutoScheduledServiceManager
    {
        private readonly IAutomobileSnapshotUpdater _snapshotUpdater;

        public AutoScheduledServiceManager(
            INoteSerializer<AutoScheduledService> noteSerializer,
            IHmmValidator<AutoScheduledService> validator,
            IHmmNoteManager noteManager,
            IEntityLookup lookupRepo,
            IAuthorProvider authorProvider,
            IAutomobileSnapshotUpdater snapshotUpdater,
            IUnitOfWork unitOfWork = null)
            : base(validator, noteManager, lookupRepo, authorProvider, unitOfWork)
        {
            ArgumentNullException.ThrowIfNull(noteSerializer);
            ArgumentNullException.ThrowIfNull(snapshotUpdater);
            NoteSerializer = noteSerializer;
            _snapshotUpdater = snapshotUpdater;
        }

        public override INoteSerializer<AutoScheduledService> NoteSerializer { get; }

        public override async Task<ProcessingResult<AutoScheduledService>> CreateAsync(AutoScheduledService entity, bool commitChanges = true)
        {
            if (entity != null)
            {
                entity.CreatedDate = entity.CreatedDate == default ? DateTime.UtcNow : entity.CreatedDate;
                entity.LastModifiedDate = DateTime.UtcNow;
            }

            var result = await base.CreateAsync(entity, commitChanges);
            if (result.Success && entity != null)
            {
                await _snapshotUpdater.RecomputeScheduleSnapshotAsync(entity.AutomobileId);
            }
            return result;
        }

        public override async Task<ProcessingResult<AutoScheduledService>> UpdateAsync(AutoScheduledService entity, bool commitChanges = true)
        {
            var result = await UpdateEntityAsync(
                entity,
                "Cannot find scheduled service in data source",
                (existing, updated) =>
                {
                    existing.Name = updated.Name;
                    existing.Type = updated.Type;
                    existing.IntervalDays = updated.IntervalDays;
                    existing.IntervalMileage = updated.IntervalMileage;
                    existing.NextDueDate = updated.NextDueDate;
                    existing.NextDueMileage = updated.NextDueMileage;
                    existing.IsActive = updated.IsActive;
                    existing.Notes = updated.Notes;
                    existing.IsDeleted = updated.IsDeleted;
                    existing.LastModifiedDate = DateTime.UtcNow;
                },
                commitChanges);

            if (result.Success && entity != null)
            {
                await _snapshotUpdater.RecomputeScheduleSnapshotAsync(entity.AutomobileId);
            }
            return result;
        }

        public async Task<ProcessingResult<PageList<AutoScheduledService>>> GetByAutomobileAsync(
            int automobileId,
            ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var subject = AutoScheduledService.GetNoteSubject(automobileId);
                var notesResult = await GetNotesAsync(new AutoScheduledService(), n => n.Subject == subject, resourceCollectionParameters);
                if (!notesResult.Success)
                {
                    return ProcessingResult<PageList<AutoScheduledService>>.Fail(notesResult.ErrorMessage, notesResult.ErrorType);
                }

                var notes = notesResult.Value;
                var entityTasks = notes.Select(async note =>
                {
                    var entityResult = await NoteSerializer.GetEntity(note);
                    return entityResult.Success ? entityResult.Value : null;
                });
                var entities = await Task.WhenAll(entityTasks);
                var list = entities.Where(e => e != null);

                var page = new PageList<AutoScheduledService>(list, notes.TotalCount, notes.CurrentPage, notes.PageSize);
                return ProcessingResult<PageList<AutoScheduledService>>.Ok(page);
            }
            catch (Exception ex)
            {
                return ProcessingResult<PageList<AutoScheduledService>>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<AutoScheduledService>> GetSoonestDueForAutomobileAsync(int automobileId)
        {
            var listResult = await GetByAutomobileAsync(automobileId);
            if (!listResult.Success)
            {
                return ProcessingResult<AutoScheduledService>.Fail(listResult.ErrorMessage, listResult.ErrorType);
            }

            var schedule = listResult.Value
                .Where(s => !s.IsDeleted && s.IsActive && s.NextDueDate.HasValue)
                .OrderBy(s => s.NextDueDate.Value)
                .FirstOrDefault();

            return schedule != null
                ? ProcessingResult<AutoScheduledService>.Ok(schedule)
                : ProcessingResult<AutoScheduledService>.NotFound("No scheduled services for this automobile");
        }
    }
}
