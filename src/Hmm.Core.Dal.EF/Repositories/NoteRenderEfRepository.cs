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
    public class NoteRenderEfRepository : RepositoryBase, IRepository<NoteRender>
    {
        public NoteRenderEfRepository(
            IHmmDataContext dataContext,
            IEntityLookup lookupRepo,
            IDateTimeProvider dateTimeProvider) : base(dataContext, lookupRepo, dateTimeProvider)
        {
        }

        public PageList<NoteRender> GetEntities(Expression<Func<NoteRender, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            return LookupRepo.GetEntities(query, resourceCollectionParameters);
        }

        public async Task<PageList<NoteRender>> GetEntitiesAsync(Expression<Func<NoteRender, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var renders = await LookupRepo.GetEntitiesAsync(query, resourceCollectionParameters);
            return renders;
        }

        public NoteRender GetEntity(int id)
        {
            try
            {
                return DataContext.Renders.Find(id);
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public async Task<NoteRender> GetEntityAsync(int id)
        {
            try
            {
                var render = await DataContext.Renders.FindAsync(id);
                return render;
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public NoteRender Add(NoteRender entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Renders.Add(entity);
                DataContext.Save();
                return entity;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public NoteRender Update(NoteRender entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            // ReSharper disable once PossibleNullReferenceException
            if (entity.Id <= 0)
            {
                ProcessMessage.Success = false;
                ProcessMessage.AddErrorMessage($"Can not update NoteRender with id {entity.Id}", true);
                return null;
            }

            try
            {
                // make sure the record exists in data source
                DataContext.Renders.Update(entity);
                DataContext.Save();
                return LookupRepo.GetEntity<NoteRender>(entity.Id);
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public bool Delete(NoteRender entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Renders.Remove(entity);
                DataContext.Save();
                return true;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return false;
            }
        }

        public async Task<NoteRender> AddAsync(NoteRender entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Renders.Add(entity);
                await DataContext.SaveAsync();
                return entity;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<NoteRender> UpdateAsync(NoteRender entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            // ReSharper disable once PossibleNullReferenceException
            if (entity.Id <= 0)
            {
                ProcessMessage.Success = false;
                ProcessMessage.AddErrorMessage($"Can not update NoteRender with id {entity.Id}", true);
                return null;
            }

            try
            {
                // make sure the record exists in data source
                DataContext.Renders.Update(entity);
                await DataContext.SaveAsync();
                return LookupRepo.GetEntity<NoteRender>(entity.Id);
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<bool> DeleteAsync(NoteRender entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Renders.Remove(entity);
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