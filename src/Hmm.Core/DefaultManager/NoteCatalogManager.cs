using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.Core.DefaultManager
{
    public class NoteCatalogManager : INoteCatalogManager
    {
        private readonly IRepository<NoteCatalog> _dataSource;
        private readonly IHmmValidator<NoteCatalog> _validator;
        private readonly INoteRenderManager _renderMan;

        public NoteCatalogManager(IRepository<NoteCatalog> dataSource, IHmmValidator<NoteCatalog> validator, INoteRenderManager renderMan)
        {
            Guard.Against<ArgumentNullException>(dataSource == null, nameof(dataSource));
            Guard.Against<ArgumentNullException>(validator == null, nameof(validator));
            Guard.Against<ArgumentNullException>(renderMan == null, nameof(renderMan));

            _dataSource = dataSource;
            _validator = validator;
            _renderMan = renderMan;
        }

        public NoteCatalog Create(NoteCatalog catalog)
        {
            if (!_validator.IsValidEntity(catalog, ProcessResult))
            {
                return null;
            }

            try
            {
                // if render does not exits, create render first
                if (_renderMan.GetEntities().All(r => r.Id != catalog.Render.Id))
                {
                    var newRender = _renderMan.Create(catalog.Render);
                    if (newRender == null)
                    {
                        ProcessResult.PropagandaResult(_renderMan.ProcessResult);
                        return null;
                    }
                }

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

        public bool BatchCreate(IEnumerable<NoteCatalog> catalogs)
        {
            Guard.Against<ArgumentNullException>(catalogs == null, nameof(catalogs));

            var success = false;
            // ReSharper disable AssignNullToNotNullAttribute
            try
            {
                var noteCatalogs = catalogs.ToList();
                success = noteCatalogs.All(catalog => _validator.IsValidEntity(catalog, ProcessResult));

                if (!success)
                {
                    return false;
                }

                if (noteCatalogs.Select(Create).Any(newCatalog => newCatalog == null))
                {
                    success = false;
                }

                return success;
            }
            catch (Exception e)
            {
                ProcessResult.WrapException(e);
                return success;
            }

            // ReSharper restore AssignNullToNotNullAttribute
        }

        public NoteCatalog Update(NoteCatalog catalog)
        {
            if (!_validator.IsValidEntity(catalog, ProcessResult))
            {
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