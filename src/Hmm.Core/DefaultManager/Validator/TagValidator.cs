using FluentValidation;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Validation;
using System;
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
            return await UniqueNameValidationHelper.IsNameUniqueAsync(
                _tagRepository,
                tag.Id,
                tagName,
                dao => dao.Name,
                dao => dao.IsActivated);
        }
    }
}