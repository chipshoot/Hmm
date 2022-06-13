using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF.Repositories
{
    public class NoteCatalogEfRepository : RepositoryBase, IRepository<NoteCatalog>
    {
        public NoteCatalogEfRepository(
            IHmmDataContext dataContext,
            IEntityLookup lookupRepo,
            IDateTimeProvider dateTimeProvider) : base(dataContext, lookupRepo, dateTimeProvider)
        {
        }

        public IQueryable<NoteCatalog> GetEntities(Expression<Func<NoteCatalog, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            return LookupRepo.GetEntities(query, resourceCollectionParameters);
        }

        public async Task<IEnumerable<NoteCatalog>> GetEntitiesAsync(Expression<Func<NoteCatalog, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var cats = await LookupRepo.GetEntitiesAsync(query, resourceCollectionParameters);
            return cats;
        }

        public NoteCatalog GetEntity(int id)
        {
            try
            {
                return DataContext.Catalogs.Find(id);
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public async Task<NoteCatalog> GetEntityAsync(int id)
        {
            try
            {
                var catalog = await DataContext.Catalogs.FindAsync(id);
                return catalog;
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public NoteCatalog Add(NoteCatalog entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
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

        public NoteCatalog Update(NoteCatalog entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

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
                var render = PropertyChecking(entity.Render);
                const string message = "Cannot find default note render.";
                ProcessMessage.Success = false;
                ProcessMessage.AddErrorMessage(message, true);
                entity.Render = render ?? throw new DataSourceException(message);

                DataContext.Catalogs.Update(entity);
                DataContext.Save();
                return LookupRepo.GetEntity<NoteCatalog>(entity.Id);
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public bool Delete(NoteCatalog entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

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

        public async Task<NoteCatalog> AddAsync(NoteCatalog entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

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

        public async Task<NoteCatalog> UpdateAsync(NoteCatalog entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

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
                var render = PropertyChecking(entity.Render);
                const string message = "Cannot find default note render.";
                ProcessMessage.Success = false;
                ProcessMessage.AddErrorMessage(message, true);
                entity.Render = render ?? throw new DataSourceException(message);

                DataContext.Catalogs.Update(entity);
                await DataContext.SaveAsync();
                var newCatalog = await LookupRepo.GetEntityAsync<NoteCatalog>(entity.Id);
                return newCatalog;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<bool> DeleteAsync(NoteCatalog entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

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