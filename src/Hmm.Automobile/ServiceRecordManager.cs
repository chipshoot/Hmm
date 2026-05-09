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
    public class ServiceRecordManager : EntityManagerBase<ServiceRecord>, IServiceRecordManager
    {
        private readonly IAutomobileSnapshotUpdater _snapshotUpdater;

        public ServiceRecordManager(
            INoteSerializer<ServiceRecord> noteSerializer,
            IHmmValidator<ServiceRecord> validator,
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

        public override INoteSerializer<ServiceRecord> NoteSerializer { get; }

        public override async Task<ProcessingResult<ServiceRecord>> CreateAsync(ServiceRecord entity, bool commitChanges = true)
        {
            if (entity != null && entity.CreatedDate == default)
            {
                entity.CreatedDate = DateTime.UtcNow;
            }

            var result = await base.CreateAsync(entity, commitChanges);
            if (result.Success && entity != null)
            {
                await _snapshotUpdater.RecomputeServiceSnapshotAsync(entity.AutomobileId);
            }
            return result;
        }

        public override async Task<ProcessingResult<ServiceRecord>> UpdateAsync(ServiceRecord entity, bool commitChanges = true)
        {
            var result = await UpdateEntityAsync(
                entity,
                "Cannot find service record in data source",
                (existing, updated) =>
                {
                    existing.Date = updated.Date;
                    existing.Mileage = updated.Mileage;
                    existing.Type = updated.Type;
                    existing.Description = updated.Description;
                    existing.Cost = updated.Cost;
                    existing.ShopName = updated.ShopName;
                    existing.Parts = updated.Parts;
                    existing.Notes = updated.Notes;
                    existing.IsDeleted = updated.IsDeleted;
                },
                commitChanges);

            if (result.Success && entity != null)
            {
                await _snapshotUpdater.RecomputeServiceSnapshotAsync(entity.AutomobileId);
            }
            return result;
        }

        public async Task<ProcessingResult<PageList<ServiceRecord>>> GetByAutomobileAsync(
            int automobileId,
            ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var subject = ServiceRecord.GetNoteSubject(automobileId);
                var notesResult = await GetNotesAsync(new ServiceRecord(), n => n.Subject == subject, resourceCollectionParameters);
                if (!notesResult.Success)
                {
                    return ProcessingResult<PageList<ServiceRecord>>.Fail(notesResult.ErrorMessage, notesResult.ErrorType);
                }

                var notes = notesResult.Value;
                var entityTasks = notes.Select(async note =>
                {
                    var entityResult = await NoteSerializer.GetEntity(note);
                    return entityResult.Success ? entityResult.Value : null;
                });
                var entities = await Task.WhenAll(entityTasks);
                var list = entities.Where(e => e != null);

                var page = new PageList<ServiceRecord>(list, notes.TotalCount, notes.CurrentPage, notes.PageSize);
                return ProcessingResult<PageList<ServiceRecord>>.Ok(page);
            }
            catch (Exception ex)
            {
                return ProcessingResult<PageList<ServiceRecord>>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<ServiceRecord>> GetMostRecentForAutomobileAsync(int automobileId)
        {
            var listResult = await GetByAutomobileAsync(automobileId);
            if (!listResult.Success)
            {
                return ProcessingResult<ServiceRecord>.Fail(listResult.ErrorMessage, listResult.ErrorType);
            }

            var record = listResult.Value
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.Mileage)
                .FirstOrDefault();

            return record != null
                ? ProcessingResult<ServiceRecord>.Ok(record)
                : ProcessingResult<ServiceRecord>.NotFound("No service records for this automobile");
        }
    }
}
