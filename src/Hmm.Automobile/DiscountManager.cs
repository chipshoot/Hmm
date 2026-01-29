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
    /// Manager for gas discount entities. Uses base class for common CRUD operations.
    /// Only UpdateAsync is overridden to handle GasDiscount-specific property copying.
    /// </summary>
    public class DiscountManager : EntityManagerBase<GasDiscount>
    {
        public DiscountManager(
            INoteSerializer<GasDiscount> noteSerializer,
            IHmmValidator<GasDiscount> validator,
            IHmmNoteManager noteManager,
            IEntityLookup lookupRepo,
            IAuthorProvider authorProvider)
            : base(validator, noteManager, lookupRepo, authorProvider)
        {
            ArgumentNullException.ThrowIfNull(noteSerializer);
            NoteSerializer = noteSerializer;
        }

        public override INoteSerializer<GasDiscount> NoteSerializer { get; }

        // GetEntitiesAsync - uses base class implementation
        // GetEntityByIdAsync - uses base class implementation
        // CreateAsync - uses base class implementation

        public override Task<ProcessingResult<GasDiscount>> UpdateAsync(GasDiscount entity)
        {
            return UpdateEntityAsync(
                entity,
                "Cannot find discount in data source",
                (existing, updated) =>
                {
                    existing.Amount = updated.Amount;
                    existing.Comment = updated.Comment;
                    existing.DiscountType = updated.DiscountType;
                    existing.IsActive = updated.IsActive;
                    existing.Program = updated.Program;
                });
        }
    }
}
