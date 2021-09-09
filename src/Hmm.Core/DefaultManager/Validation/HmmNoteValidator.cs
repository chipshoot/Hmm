using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Validation;

namespace Hmm.Core.DefaultManager.Validation
{
    public class HmmNoteValidator : ValidatorBase<HmmNote>
    {
        public HmmNoteValidator(IEntityLookup lookupRepo) : base(lookupRepo)
        {
        }

        public override bool IsValid(HmmNote entity, bool isNewEntity)
        {
            if (entity == null)
            {
                ValidationErrors.Add("Error: null note found.");
                return false;
            }

            // make sure note author is exists
            if (isNewEntity)
            {
                if (!IsAuthorValid(entity.Author))
                {
                    return false;
                }
            }

            if (!isNewEntity)
            {
                if (entity.Id <= 0)
                {
                    ValidationErrors.Add($"The note get invalid id {entity.Id}");
                    return false;
                }

                var rec = LookupRepo.GetEntity<HmmNote>(entity.Id);
                if (rec == null)
                {
                    ValidationErrors.Add($"Cannot find note with id {entity.Id} from data source");
                    return false;
                }

                // check if user want to update author for note which does not allowed by system
                var newAuthorId = entity.Author.Id;
                if(rec.Author.Id != newAuthorId)
                {
                    ValidationErrors.Add($"Cannot update note: {entity.Id}'s author. current author id: {rec.Id}, new author id: {newAuthorId}");
                    return false;
                }
            }

            return true;
        }

        private bool IsAuthorValid(User author)
        {
            // make sure note author is exists
            if (author == null || author.Id <= 0)
            {
                ValidationErrors.Add("Error: invalid author attached to note.");
                return false;
            }

            var savedAuthor = LookupRepo.GetEntity<User>(author.Id);
            if (savedAuthor == null)
            {
                ValidationErrors.Add("Error: cannot find author from data source.");
                return false;
            }

            return true;
        }
    }
}