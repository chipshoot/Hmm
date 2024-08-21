using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.Map;
using Hmm.Core.Map.DbEntity;

namespace Hmm.Core.DefaultManager
{
    public class NoteCatalogManager : INoteCatalogManager
    {
        private readonly IRepository<NoteCatalogDao> _catalogRepository;
        private readonly IMapper _mapper;
        private readonly ValidatorBase<NoteCatalog> _validator;

        public NoteCatalogManager(IRepository<NoteCatalogDao> dataSource, IMapper mapper)
        {
            Guard.Against<ArgumentNullException>(dataSource == null, nameof(dataSource));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));

            _mapper = mapper;
            _catalogRepository = dataSource;
            _validator = new NoteCatalogValidator(this);
        }

        public async Task<PageList<NoteCatalog>> GetEntitiesAsync(Expression<Func<NoteCatalog, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                Expression<Func<NoteCatalogDao, bool>> daoQuery = null;
                if (query != null)
                {
                    daoQuery = ExpressionMapper<NoteCatalog, NoteCatalogDao>.MapExpression(query);
                }

                var catalogDaos = await _catalogRepository.GetEntitiesAsync(daoQuery, resourceCollectionParameters);
                var catalogs = _mapper.Map<PageList<NoteCatalog>>(catalogDaos);
                return catalogs;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<NoteCatalog> GetEntityByIdAsync(int id)
        {
            try
            {
                var catalogDao = await _catalogRepository.GetEntityAsync(id);
                var catalog = _mapper.Map<NoteCatalog>(catalogDao);
                switch (catalog)
                {
                    case null:
                        ProcessResult.AddErrorMessage("Cannot map CatalogDao to Catalog");
                        return null;
                    default:
                        return catalog;
                }
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<NoteCatalog> CreateAsync(NoteCatalog catalog)
        {
            try
            {
                ProcessResult.Rest();
                var isValid = await _validator.IsValidEntityAsync(catalog, ProcessResult);
                if (!isValid)
                {
                    return null;
                }

                var catalogDao = _mapper.Map<NoteCatalogDao>(catalog);
                if (catalogDao == null)
                {
                    ProcessResult.AddErrorMessage("Cannot convert NoteCatalog to NoteCatalogDao");
                    return null;
                }

                var addedCatalogDao = await _catalogRepository.AddAsync(catalogDao);
                if (addedCatalogDao == null)
                {
                    ProcessResult.PropagandaResult(_catalogRepository.ProcessMessage);
                    return null;
                }

                catalog.Id = addedCatalogDao.Id;
                return catalog;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<NoteCatalog> UpdateAsync(NoteCatalog catalog)
        {
            try
            {
                if (catalog == null)
                {
                    return null;
                }

                ProcessResult.Rest();
                var isValid = await _validator.IsValidEntityAsync(catalog, ProcessResult);
                if (!isValid)
                {
                    return null;
                }

                // update catalog record
                var catalogDao = _mapper.Map<NoteCatalogDao>(catalog);
                if (catalogDao == null)
                {
                    ProcessResult.AddErrorMessage("Cannot map NoteCatalog to NoteCatalogDao");
                    return null;
                }
                var savedCatalogDao = await _catalogRepository.GetEntityAsync(catalog.Id);
                if (savedCatalogDao == null)
                {
                    ProcessResult.AddErrorMessage($"Cannot found catalog: {catalog.Name} for updating.");
                    return null;
                }
                var updatedCatalog = await _catalogRepository.UpdateAsync(catalogDao);
                if (updatedCatalog == null)
                {
                    ProcessResult.PropagandaResult(_catalogRepository.ProcessMessage);
                }

                return catalog;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public ProcessingResult ProcessResult { get; } = new();
    }
}