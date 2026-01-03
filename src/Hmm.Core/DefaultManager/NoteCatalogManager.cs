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
        private readonly IHmmValidator<NoteCatalog> _validator;
        private readonly IEntityLookup _lookup;

        public NoteCatalogManager(IRepository<NoteCatalogDao> dataSource, IMapper mapper, IEntityLookup lookup)
        {
            Guard.Against<ArgumentNullException>(dataSource == null, nameof(dataSource));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));
            Guard.Against<ArgumentNullException>(lookup == null, nameof(lookup));

            _mapper = mapper;
            _catalogRepository = dataSource;
            _validator = new NoteCatalogValidator(this);
            _lookup = lookup;
        }

        public async Task<ProcessingResult<PageList<NoteCatalog>>> GetEntitiesAsync(Expression<Func<NoteCatalog, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
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
                return ProcessingResult<PageList<NoteCatalog>>.Ok(catalogs);
            }
            catch (Exception ex)
            {
                return ProcessingResult<PageList<NoteCatalog>>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<NoteCatalog>> GetEntityByIdAsync(int id)
        {
            var catalogDaoResult = await _lookup.GetEntityAsync<NoteCatalogDao>(id);

            if (!catalogDaoResult.Success)
            {
                return ProcessingResult<NoteCatalog>.Fail(catalogDaoResult.ErrorMessage, catalogDaoResult.ErrorType);
            }

            var catalogDao = catalogDaoResult.Value;
            var catalog = _mapper.Map<NoteCatalog>(catalogDao);
            if (catalog == null)
            {
                return ProcessingResult<NoteCatalog>.Fail("Cannot convert NoteCatalogDao to NoteCatalog");
            }

            return ProcessingResult<NoteCatalog>.Ok(catalog);
        }

        public async Task<ProcessingResult<NoteCatalog>> CreateAsync(NoteCatalog catalog)
        {
            try
            {
                var validationResult = await _validator.ValidateEntityAsync(catalog);
                if (!validationResult.Success)
                {
                    return ProcessingResult<NoteCatalog>.Invalid(validationResult.GetWholeMessage());
                }

                var catalogDao = _mapper.Map<NoteCatalogDao>(catalog);
                if (catalogDao == null)
                {
                    return ProcessingResult<NoteCatalog>.Fail("Cannot convert NoteCatalog to NoteCatalogDao");
                }

                var addedCatalogDaoResult = await _catalogRepository.AddAsync(catalogDao);
                if (!addedCatalogDaoResult.Success)
                {
                    return ProcessingResult<NoteCatalog>.Fail(addedCatalogDaoResult.ErrorMessage, addedCatalogDaoResult.ErrorType);
                }

                var createdCatalog = _mapper.Map<NoteCatalog>(addedCatalogDaoResult.Value);
                return ProcessingResult<NoteCatalog>.Ok(createdCatalog);
            }
            catch (Exception ex)
            {
                return ProcessingResult<NoteCatalog>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<NoteCatalog>> UpdateAsync(NoteCatalog catalog)
        {
            try
            {
                var validationResult = await _validator.ValidateEntityAsync(catalog);
                if (!validationResult.Success)
                {
                    return ProcessingResult<NoteCatalog>.Invalid(validationResult.GetWholeMessage());
                }

                var catalogDao = _mapper.Map<NoteCatalogDao>(catalog);
                if (catalogDao == null)
                {
                    return ProcessingResult<NoteCatalog>.Fail("Cannot convert NoteCatalog to NoteCatalogDao");
                }

                var savedCatalogResult = await _lookup.GetEntityAsync<NoteCatalogDao>(catalog.Id);
                if (!savedCatalogResult.Success)
                {
                    return ProcessingResult<NoteCatalog>.NotFound($"Cannot update catalog: {catalog.Name}, because system cannot find it in data source");
                }

                var updatedCatalogDaoResult = await _catalogRepository.UpdateAsync(catalogDao);
                if (!updatedCatalogDaoResult.Success)
                {
                    return ProcessingResult<NoteCatalog>.Fail(updatedCatalogDaoResult.ErrorMessage, updatedCatalogDaoResult.ErrorType);
                }

                var updatedCatalog = _mapper.Map<NoteCatalog>(updatedCatalogDaoResult.Value);
                if (updatedCatalog == null)
                {
                    return ProcessingResult<NoteCatalog>.Fail("Cannot convert NoteCatalogDao to NoteCatalog");
                }

                return ProcessingResult<NoteCatalog>.Ok(updatedCatalog);
            }
            catch (Exception ex)
            {
                return ProcessingResult<NoteCatalog>.FromException(ex);
            }
        }
    }
}