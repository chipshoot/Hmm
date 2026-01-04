using FluentValidation;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hmm.Core.DefaultManager.Validator
{
    public class TagValidator : ValidatorBase<Tag>
    {
        private readonly ICompositeEntityRepository<TagDao, HmmNoteDao> _tagRepository;

        public TagValidator(ICompositeEntityRepository<TagDao, HmmNoteDao> tagRepository)
        {
            ArgumentNullException.ThrowIfNull(tagRepository);
            _tagRepository = tagRepository;

            RuleFor(n => n.Name).NotNull().Length(1, 200);
            RuleFor(n => n.Name).MustAsync(TagNameUnique).WithMessage(n => $"Tag name {n.Name} is not unique");
            RuleFor(n => n.IsActivated).NotNull();
            RuleFor(n => n.Description).Length(1, 1000);
        }

        private async Task<bool> TagNameUnique(Tag tag, string tagName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(tagName))
            {
                return false;
            }

            var savedTagResult = await _tagRepository.GetEntityAsync(tag.Id);

            // create new tag, make sure tag name is unique
            var tname = tagName.Trim().ToLower();
            if (!savedTagResult.Success || savedTagResult.IsNotFound)
            {
                // Creating new tag - check for existing tag names
                var sameNameTagsResult = await _tagRepository.GetEntitiesAsync(t => t.Name.ToLower() == tname && t.IsActivated);
                if (sameNameTagsResult.Success && !sameNameTagsResult.IsNotFound)
                {
                    return false;
                }
            }
            else
            {
                // Updating existing tag - check for conflicts with other tags
                var tagWithNamesResult = await _tagRepository.GetEntitiesAsync(t => t.Name.ToLower() == tname && t.Id != tag.Id && t.IsActivated);
                if (tagWithNamesResult.Success && !tagWithNamesResult.IsNotFound)
                {
                    return !tagWithNamesResult.Value.Any();
                }
            }

            return true;
        }
    }
}