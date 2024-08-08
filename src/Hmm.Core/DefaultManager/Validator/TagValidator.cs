using FluentValidation;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hmm.Core.DefaultManager.Validator
{
    public class TagValidator : ValidatorBase<Tag>
    {
        private readonly ITagManager _tagManager;

        public TagValidator(ITagManager tagManager)
        {
            Guard.Against<ArgumentNullException>(tagManager == null, nameof(tagManager));
            _tagManager = tagManager;

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

            var savedTag = await _tagManager.GetTagByIdAsync(tag.Id);

            // create new user, make sure account name is unique
            var tname = tagName.Trim();
            if (savedTag == null)
            {
                var sameNameTags= await _tagManager.GetEntitiesAsync(t => t.Name.Equals(tname, StringComparison.CurrentCultureIgnoreCase));
                if (sameNameTags.Any())
                {
                    return false;
                }
            }
            else
            {
                var tagWithNames = await _tagManager.GetEntitiesAsync(t => t.Name.Equals(tname, StringComparison.CurrentCultureIgnoreCase) && t.Id != tag.Id);
                return !tagWithNames.Any();
            }

            return true;
        }
    }
}