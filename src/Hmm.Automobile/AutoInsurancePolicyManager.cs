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
    public class AutoInsurancePolicyManager : EntityManagerBase<AutoInsurancePolicy>, IAutoInsurancePolicyManager
    {
        private readonly IAutomobileSnapshotUpdater _snapshotUpdater;

        public AutoInsurancePolicyManager(
            INoteSerializer<AutoInsurancePolicy> noteSerializer,
            IHmmValidator<AutoInsurancePolicy> validator,
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

        public override INoteSerializer<AutoInsurancePolicy> NoteSerializer { get; }

        public override async Task<ProcessingResult<AutoInsurancePolicy>> CreateAsync(AutoInsurancePolicy entity, bool commitChanges = true)
        {
            if (entity != null)
            {
                entity.CreatedDate = entity.CreatedDate == default ? DateTime.UtcNow : entity.CreatedDate;
                entity.LastModifiedDate = DateTime.UtcNow;
            }

            var result = await base.CreateAsync(entity, commitChanges);
            if (result.Success && entity != null)
            {
                await _snapshotUpdater.RecomputeInsuranceSnapshotAsync(entity.AutomobileId);
            }
            return result;
        }

        public override async Task<ProcessingResult<AutoInsurancePolicy>> UpdateAsync(AutoInsurancePolicy entity, bool commitChanges = true)
        {
            var result = await UpdateEntityAsync(
                entity,
                "Cannot find auto insurance policy in data source",
                (existing, updated) =>
                {
                    existing.Provider = updated.Provider;
                    existing.PolicyNumber = updated.PolicyNumber;
                    existing.EffectiveDate = updated.EffectiveDate;
                    existing.ExpiryDate = updated.ExpiryDate;
                    existing.Premium = updated.Premium;
                    existing.Deductible = updated.Deductible;
                    existing.Coverage = updated.Coverage;
                    existing.Notes = updated.Notes;
                    existing.IsActive = updated.IsActive;
                    existing.IsDeleted = updated.IsDeleted;
                    existing.LastModifiedDate = DateTime.UtcNow;
                },
                commitChanges);

            if (result.Success && entity != null)
            {
                await _snapshotUpdater.RecomputeInsuranceSnapshotAsync(entity.AutomobileId);
            }
            return result;
        }

        public async Task<ProcessingResult<PageList<AutoInsurancePolicy>>> GetByAutomobileAsync(
            int automobileId,
            ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var subject = AutoInsurancePolicy.GetNoteSubject(automobileId);
                var notesResult = await GetNotesAsync(new AutoInsurancePolicy(), n => n.Subject == subject, resourceCollectionParameters);
                if (!notesResult.Success)
                {
                    return ProcessingResult<PageList<AutoInsurancePolicy>>.Fail(notesResult.ErrorMessage, notesResult.ErrorType);
                }

                var notes = notesResult.Value;
                var entityTasks = notes.Select(async note =>
                {
                    var entityResult = await NoteSerializer.GetEntity(note);
                    return entityResult.Success ? entityResult.Value : null;
                });
                var entities = await Task.WhenAll(entityTasks);
                var list = entities.Where(e => e != null);

                var page = new PageList<AutoInsurancePolicy>(list, notes.TotalCount, notes.CurrentPage, notes.PageSize);
                return ProcessingResult<PageList<AutoInsurancePolicy>>.Ok(page);
            }
            catch (Exception ex)
            {
                return ProcessingResult<PageList<AutoInsurancePolicy>>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<AutoInsurancePolicy>> GetActiveForAutomobileAsync(int automobileId)
        {
            var listResult = await GetByAutomobileAsync(automobileId);
            if (!listResult.Success)
            {
                return ProcessingResult<AutoInsurancePolicy>.Fail(listResult.ErrorMessage, listResult.ErrorType);
            }

            var now = DateTime.UtcNow;
            var active = listResult.Value
                .Where(p => !p.IsDeleted && p.IsActive && p.EffectiveDate <= now && p.ExpiryDate > now)
                .OrderByDescending(p => p.EffectiveDate)
                .ThenByDescending(p => p.Id)
                .FirstOrDefault();

            return active != null
                ? ProcessingResult<AutoInsurancePolicy>.Ok(active)
                : ProcessingResult<AutoInsurancePolicy>.NotFound("No active insurance policy for this automobile");
        }
    }
}
