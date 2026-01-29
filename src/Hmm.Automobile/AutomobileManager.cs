using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    /// <summary>
    /// Manager for automobile entities. Uses base class for common CRUD operations.
    /// Only UpdateAsync is overridden to handle AutomobileInfo-specific property copying.
    /// </summary>
    public class AutomobileManager : EntityManagerBase<AutomobileInfo>
    {
        public AutomobileManager(
            INoteSerializer<AutomobileInfo> noteSerializer,
            IHmmValidator<AutomobileInfo> validator,
            IHmmNoteManager noteManager,
            IEntityLookup lookupRepo,
            IAuthorProvider authorProvider)
            : base(validator, noteManager, lookupRepo, authorProvider)
        {
            ArgumentNullException.ThrowIfNull(noteSerializer);
            NoteSerializer = noteSerializer;
        }

        public override INoteSerializer<AutomobileInfo> NoteSerializer { get; }

        // GetEntitiesAsync - uses base class implementation
        // GetEntityByIdAsync - uses base class implementation
        // CreateAsync - uses base class implementation

        public override Task<ProcessingResult<AutomobileInfo>> UpdateAsync(AutomobileInfo entity)
        {
            return UpdateEntityAsync(
                entity,
                "Cannot find automobile in data source",
                (existing, updated) =>
                {
                    existing.Brand = updated.Brand;
                    existing.Maker = updated.Maker;
                    existing.MeterReading = updated.MeterReading;
                    existing.Year = updated.Year;
                    existing.Color = updated.Color;
                    existing.Plate = updated.Plate;
                });
        }
    }
}