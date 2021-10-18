using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using Hmm.Utility.Dal.Query;

namespace Hmm.Core.DefaultManager
{
    public class NoteCatalogManager : INoteCatalogManager
    {
        private readonly IRepository<NoteCatalog> _dataSource;
        private readonly IHmmValidator<NoteCatalog> _validator;
        private readonly IEntityLookup _lookupRepo;

        public NoteCatalogManager(IRepository<NoteCatalog> dataSource, IHmmValidator<NoteCatalog> validator, IEntityLookup lookupRepo)
        {
            Guard.Against<ArgumentNullException>(dataSource == null, nameof(dataSource));
            Guard.Against<ArgumentNullException>(validator == null, nameof(validator));
            Guard.Against<ArgumentNullException>(lookupRepo == null, nameof(lookupRepo));

            _dataSource = dataSource;
            _validator = validator;
            _lookupRepo = lookupRepo;
        }

        public NoteCatalog Create(NoteCatalog catalog)
        {
            if (catalog == null || !_validator.IsValidEntity(catalog, ProcessResult))
            {
                return null;
            }

            try
            {
                var addedCatalog = _dataSource.Add(catalog);
                if (addedCatalog == null)
                {
                    ProcessResult.PropagandaResult(_dataSource.ProcessMessage);
                }
                return addedCatalog;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public NoteCatalog Update(NoteCatalog catalog)
        {
            if (catalog == null || !_validator.IsValidEntity(catalog, ProcessResult))
            {
                return null;
            }

            // check if note render exists in data source
            if (_lookupRepo.GetEntity<NoteRender>(catalog.Render.Id) == null)
            {
                ProcessResult.AddErrorMessage($"Cannot update catalog: {catalog.Name}, because note render does not exists in data source");
                return null;

            }
            // update catalog record
            if (GetEntities().All(c => c.Id != catalog.Id))
            {
                ProcessResult.AddErrorMessage($"Cannot update catalog: {catalog.Name}, because system cannot find it in data source");
                return null;
            }
            var updatedCatalog = _dataSource.Update(catalog);
            if (updatedCatalog == null)
            {
                ProcessResult.PropagandaResult(_dataSource.ProcessMessage);
            }

            return updatedCatalog;
        }

        public IEnumerable<NoteCatalog> GetEntities()
        {
            try
            {
                var catalogs = _dataSource.GetEntities();
                return catalogs;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public ProcessingResult ProcessResult { get; } = new ProcessingResult();
    }
}