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
    public class TagEfRepository(
        IHmmDataContext dataContext,
        IEntityLookup lookupRepository,
        IDateTimeProvider dateTimeProvider)
        : RepositoryBase(dataContext, lookupRepository, dateTimeProvider), IRepository<TagDao>
    {
        public PageList<TagDao> GetEntities(Expression<Func<TagDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            return LookupRepository.GetEntities(query, resourceCollectionParameters);
        }

        public async Task<PageList<TagDao>> GetEntitiesAsync(Expression<Func<TagDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var cats = await LookupRepository.GetEntitiesAsync(query, resourceCollectionParameters);
            return cats;
        }

        public TagDao GetEntity(int id)
        {
            try
            {
                ProcessMessage.Rest();
                return DataContext.Tags.Find(id);
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public async Task<TagDao> GetEntityAsync(int id)
        {
            try
            {
                ProcessMessage.Rest();
                var tag = await DataContext.Tags.FindAsync(id);
                return tag;
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public TagDao Add(TagDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                ProcessMessage.Rest();

                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Tags.Add(entity);
                DataContext.Save();
                return entity;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public TagDao Update(TagDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();

            // ReSharper disable once PossibleNullReferenceException
            if (entity.Id <= 0)
            {
                ProcessMessage.Success = false;
                ProcessMessage.AddErrorMessage($"Can not update Tag with id {entity.Id}", true);
                return null;
            }

            try
            {
                DataContext.Tags.Update(entity);
                DataContext.Save();
                return LookupRepository.GetEntity<TagDao>(entity.Id);
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public bool Delete(TagDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Tags.Remove(entity);
                DataContext.Save();
                return true;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return false;
            }
        }

        public async Task<TagDao> AddAsync(TagDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Tags.Add(entity);
                await DataContext.SaveAsync();
                return entity;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<TagDao> UpdateAsync(TagDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();

            // ReSharper disable once PossibleNullReferenceException
            if (entity.Id <= 0)
            {
                ProcessMessage.Success = false;
                ProcessMessage.AddErrorMessage($"Can not update Tag with id {entity.Id}", true);
                return null;
            }

            try
            {
                DataContext.Tags.Update(entity);
                await DataContext.SaveAsync();
                var newTag = await LookupRepository.GetEntityAsync<TagDao>(entity.Id);
                return newTag;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<bool> DeleteAsync(TagDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();
            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Tags.Remove(entity);
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