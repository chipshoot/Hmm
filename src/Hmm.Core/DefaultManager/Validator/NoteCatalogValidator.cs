using System;
using System.Linq;
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
            if (string.IsNullOrEmpty(catalogName))
            {
                return false;
            }

            var savedCatalogResult = await _catalogRepository.GetEntityAsync(catalog.Id);

            // create new catalog, make sure catalog name is unique
            var cname = catalogName.Trim().ToLower();
            if (!savedCatalogResult.Success || savedCatalogResult.Value == null)
            {
                // Creating new catalog - check for existing catalog names
                var sameNameCatalogsResult = await _catalogRepository.GetEntitiesAsync(c => c.Name.ToLower() == cname);
                if (sameNameCatalogsResult.Success && !sameNameCatalogsResult.IsNotFound)
                {
                    return false;
                }
            }
            else
            {
                // Updating existing catalog - check for conflicts with other catalogs
                var catalogWithNamesResult = await _catalogRepository.GetEntitiesAsync(c => c.Name.ToLower() == cname && c.Id != catalog.Id);
                if (catalogWithNamesResult.Success && !catalogWithNamesResult.IsNotFound)
                {
                    return !catalogWithNamesResult.Value.Any();
                }
            }

            return true;
        }
    }
}