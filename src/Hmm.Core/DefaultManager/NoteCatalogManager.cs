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

        public NoteCatalogManager(IRepository<NoteCatalogDao> dataSource, IMapper mapper, IEntityLookup lookup, IHmmValidator<NoteCatalog> validator)
        {
            ArgumentNullException.ThrowIfNull(dataSource);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(lookup);
            ArgumentNullException.ThrowIfNull(validator);

            _mapper = mapper;
            _catalogRepository = dataSource;
            _validator = validator;
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

                var catalogDaosResult = await _catalogRepository.GetEntitiesAsync(daoQuery, resourceCollectionParameters);
                if (!catalogDaosResult.Success)
                {
                    return ProcessingResult<PageList<NoteCatalog>>.Fail(catalogDaosResult.ErrorMessage, catalogDaosResult.ErrorType);
                }

                var catalogs = _mapper.Map<PageList<NoteCatalog>>(catalogDaosResult.Value);
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

            return _mapper.MapWithNullCheck<NoteCatalogDao, NoteCatalog>(catalogDaoResult.Value);
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

                var catalogDaoResult = _mapper.MapWithNullCheck<NoteCatalog, NoteCatalogDao>(catalog);
                if (!catalogDaoResult.Success)
                {
                    return ProcessingResult<NoteCatalog>.Fail(catalogDaoResult.ErrorMessage);
                }

                var addedCatalogDaoResult = await _catalogRepository.AddAsync(catalogDaoResult.Value);
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

                var catalogDaoResult = _mapper.MapWithNullCheck<NoteCatalog, NoteCatalogDao>(catalog);
                if (!catalogDaoResult.Success)
                {
                    return ProcessingResult<NoteCatalog>.Fail(catalogDaoResult.ErrorMessage);
                }

                var updatedCatalogDaoResult = await _catalogRepository.UpdateAsync(catalogDaoResult.Value);
                if (!updatedCatalogDaoResult.Success)
                {
                    return ProcessingResult<NoteCatalog>.Fail(updatedCatalogDaoResult.ErrorMessage, updatedCatalogDaoResult.ErrorType);
                }

                return _mapper.MapWithNullCheck<NoteCatalogDao, NoteCatalog>(updatedCatalogDaoResult.Value);
            }
            catch (Exception ex)
            {
                return ProcessingResult<NoteCatalog>.FromException(ex);
            }
        }
    }
}