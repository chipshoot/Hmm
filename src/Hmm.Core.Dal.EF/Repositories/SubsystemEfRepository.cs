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
    public class SubsystemEfRepository : RepositoryBase, IRepository<Subsystem>
    {
        public SubsystemEfRepository(IHmmDataContext dataContext, IEntityLookup lookupRepo, IDateTimeProvider dateTimeProvider) : base(dataContext, lookupRepo, dateTimeProvider)
        {
        }

        public IQueryable<Subsystem> GetEntities(Expression<Func<Subsystem, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            return LookupRepo.GetEntities(query, resourceCollectionParameters);
        }

        public async Task<IEnumerable<Subsystem>> GetEntitiesAsync(Expression<Func<Subsystem, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var systems = await LookupRepo.GetEntitiesAsync(query, resourceCollectionParameters);
            return systems;
        }

        public Subsystem GetEntity(int id)
        {
            try
            {
                return DataContext.Subsystems.Find(id);
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public async Task<Subsystem> GetEntityAsync(int id)
        {
            try
            {
                var system = await DataContext.Subsystems.FindAsync(id);
                return system;
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public Subsystem Add(Subsystem entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // ReSharper disable once PossibleNullReferenceException
                if (entity.DefaultAuthor != null && DataContext.Authors.Any(u => u.Id == entity.DefaultAuthor.Id))
                {
                    DataContext.Authors.Attach(entity.DefaultAuthor);
                }
                foreach (var cat in entity.NoteCatalogs)
                {
                    if (DataContext.Catalogs.Any(c => c.Id == cat.Id))
                    {
                        DataContext.Catalogs.Attach(cat);
                    }

                    if (DataContext.Renders.Any(r => r.Id == cat.Render.Id))
                    {
                        DataContext.Renders.Attach(cat.Render);
                    }
                }
                DataContext.Subsystems.Add(entity);

                DataContext.Save();
                return entity;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public Subsystem Update(Subsystem entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            // ReSharper disable once PossibleNullReferenceException
            if (entity.Id <= 0)
            {
                ProcessMessage.Success = false;
                ProcessMessage.AddErrorMessage($"Can not update Subsystem with id {entity.Id}", true);
                return null;
            }

            try
            {
                // make sure the record exists in data source
                DataContext.Subsystems.Update(entity);
                DataContext.Save();
                return LookupRepo.GetEntity<Subsystem>(entity.Id);
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public bool Delete(Subsystem entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Subsystems.Remove(entity);
                DataContext.Save();
                return true;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return false;
            }
        }

        public async Task<Subsystem> AddAsync(Subsystem entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // ReSharper disable once PossibleNullReferenceException
                if (entity.DefaultAuthor != null && DataContext.Authors.Any(u => u.Id == entity.DefaultAuthor.Id))
                {
                    DataContext.Authors.Attach(entity.DefaultAuthor);
                }
                foreach (var cat in entity.NoteCatalogs)
                {
                    if (DataContext.Catalogs.Any(c => c.Id == cat.Id))
                    {
                        DataContext.Catalogs.Attach(cat);
                    }

                    if (DataContext.Renders.Any(r => r.Id == cat.Render.Id))
                    {
                        DataContext.Renders.Attach(cat.Render);
                    }
                }
                DataContext.Subsystems.Add(entity);

                await DataContext.SaveAsync();
                return entity;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<Subsystem> UpdateAsync(Subsystem entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            // ReSharper disable once PossibleNullReferenceException
            if (entity.Id <= 0)
            {
                ProcessMessage.Success = false;
                ProcessMessage.AddErrorMessage($"Can not update Subsystem with id {entity.Id}", true);
                return null;
            }

            try
            {
                // make sure the record exists in data source
                DataContext.Subsystems.Update(entity);
                await DataContext.SaveAsync();
                return LookupRepo.GetEntity<Subsystem>(entity.Id);
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<bool> DeleteAsync(Subsystem entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Subsystems.Remove(entity);
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