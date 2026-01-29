using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Validation;

namespace Hmm.Core.DefaultManager.Validator
{
    public class NoteCatalogValidator : ValidatorBase<NoteCatalog>
    {
        private readonly IRepository<NoteCatalogDao> _catalogRepository;

        public NoteCatalogValidator(IRepository<NoteCatalogDao> catalogRepository)
        {
            ArgumentNullException.ThrowIfNull(catalogRepository);
            _catalogRepository = catalogRepository;

            RuleFor(c => c.Name).NotNull().Length(1, 200);
            RuleFor(c => c.Name).MustAsync(CatalogNameUnique).WithMessage(n => $"Note catalog name {n.Name} is not unique");
            RuleFor(c => c.Schema).NotNull();
            RuleFor(c => c.IsDefault).NotNull();
            RuleFor(c => c.Description).Length(0, 1000);
        }

        private async Task<bool> CatalogNameUnique(NoteCatalog catalog, string catalogName, CancellationToken cancellationToken)
        {
            // NoteCatalog doesn't have IsActivated filter - all catalogs are checked
            return await UniqueNameValidationHelper.IsNameUniqueAsync<NoteCatalog, NoteCatalogDao>(
                _catalogRepository,
                catalog.Id,
                catalogName,
                dao => dao.Name,
                additionalFilter: null);
        }
    }
}