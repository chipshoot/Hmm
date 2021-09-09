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
    public class NoteCatalogEfRepository : RepositoryBase, IRepository<NoteCatalog>
    {
        public NoteCatalogEfRepository(
            IHmmDataContext dataContext,
            IEntityLookup lookupRepo,
            IDateTimeProvider dateTimeProvider) : base(dataContext, lookupRepo, dateTimeProvider)
        {
        }

        public IQueryable<NoteCatalog> GetEntities(Expression<Func<NoteCatalog, bool>> query = null)
        {
            return LookupRepo.GetEntities(query);
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

        public void Flush()
        {
            DataContext.Save();
        }
    }
}