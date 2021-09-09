using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Linq.Expressions;

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

        public IQueryable<NoteRender> GetEntities(Expression<Func<NoteRender, bool>> query = null)
        {
            return LookupRepo.GetEntities(query);
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

        public void Flush()
        {
            DataContext.Save();
        }
    }
}