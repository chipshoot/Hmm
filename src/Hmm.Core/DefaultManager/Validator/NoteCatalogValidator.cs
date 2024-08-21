using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Validation;

namespace Hmm.Core.DefaultManager.Validator
{
    public class NoteCatalogValidator : ValidatorBase<NoteCatalog>
    {
        private readonly INoteCatalogManager _catalogManager;

        public NoteCatalogValidator(INoteCatalogManager catalogManager)
        {
            Guard.Against<ArgumentNullException>(catalogManager == null, nameof(catalogManager));
            _catalogManager = catalogManager;

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

            var savedCatalog = await _catalogManager.GetEntityByIdAsync(catalog.Id);

            // create new user, make sure account name is unique
            var cname = catalogName.Trim();
            if (savedCatalog == null)
            {
                var sameNameCatalogs= await _catalogManager.GetEntitiesAsync(c => c.Name.Equals(cname, StringComparison.CurrentCultureIgnoreCase));
                if (sameNameCatalogs.Any())
                {
                    return false;
                }
            }
            else
            {
                var catalogWithNames = await _catalogManager.GetEntitiesAsync(c => c.Name.Equals(cname, StringComparison.CurrentCultureIgnoreCase) && c.Id != catalog.Id);
                return !catalogWithNames.Any();
            }

            return true;
        }
    }
}