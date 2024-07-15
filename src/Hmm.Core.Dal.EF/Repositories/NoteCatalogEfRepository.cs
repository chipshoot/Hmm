// Ignore Spelling: Ef

using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF.Repositories
{
    public class NoteCatalogEfRepository(
        IHmmDataContext dataContext,
        IEntityLookup lookupRepository,
        IDateTimeProvider dateTimeProvider)
        : RepositoryBase(dataContext, lookupRepository, dateTimeProvider), IRepository<NoteCatalogDao>
    {
        public PageList<NoteCatalogDao> GetEntities(Expression<Func<NoteCatalogDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            return LookupRepository.GetEntities(query, resourceCollectionParameters);
        }

        public async Task<PageList<NoteCatalogDao>> GetEntitiesAsync(Expression<Func<NoteCatalogDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var cats = await LookupRepository.GetEntitiesAsync(query, resourceCollectionParameters);
            return cats;
        }

        public NoteCatalogDao GetEntity(int id)
        {
            try
            {
                ProcessMessage.Rest();
                return DataContext.Catalogs.Find(id);
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public async Task<NoteCatalogDao> GetEntityAsync(int id)
        {
            try
            {
                ProcessMessage.Rest();
                var catalog = await DataContext.Catalogs.FindAsync(id);
                return catalog;
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public NoteCatalogDao Add(NoteCatalogDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                ProcessMessage.Rest();

                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Catalogs.Add(entity);
                DataContext.Save();
                return entity;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public NoteCatalogDao Update(NoteCatalogDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();

            // ReSharper disable once PossibleNullReferenceException
            if (entity.Id <= 0)
            {
                ProcessMessage.Success = false;
                ProcessMessage.AddErrorMessage($"Can not update NoteCatalog with id {entity.Id}", true);
                return null;
            }

            try
            {
                // check if need apply default render
                const string message = "Cannot find default note render.";
                ProcessMessage.Success = false;
                ProcessMessage.AddErrorMessage(message, true);

                DataContext.Catalogs.Update(entity);
                DataContext.Save();
                return LookupRepository.GetEntity<NoteCatalogDao>(entity.Id);
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public bool Delete(NoteCatalogDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Catalogs.Remove(entity);
                DataContext.Save();
                return true;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return false;
            }
        }

        public async Task<NoteCatalogDao> AddAsync(NoteCatalogDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Catalogs.Add(entity);
                await DataContext.SaveAsync();
                return entity;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<NoteCatalogDao> UpdateAsync(NoteCatalogDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();

            // ReSharper disable once PossibleNullReferenceException
            if (entity.Id <= 0)
            {
                ProcessMessage.Success = false;
                ProcessMessage.AddErrorMessage($"Can not update NoteCatalog with id {entity.Id}", true);
                return null;
            }

            try
            {
                // check if need apply default render
                const string message = "Cannot find default note render.";
                ProcessMessage.Success = false;
                ProcessMessage.AddErrorMessage(message, true);

                DataContext.Catalogs.Update(entity);
                await DataContext.SaveAsync();
                var newCatalog = await LookupRepository.GetEntityAsync<NoteCatalogDao>(entity.Id);
                return newCatalog;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<bool> DeleteAsync(NoteCatalogDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();
            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Catalogs.Remove(entity);
                await DataContext.SaveAsync();
                return true;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return false;
            }
        }

        public void Flush()
        {
            DataContext.Save();
        }
    }
}