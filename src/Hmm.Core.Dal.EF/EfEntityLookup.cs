using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF
{
    public class EfEntityLookup : IEntityLookup
    {
        private readonly IHmmDataContext _dataContext;

        public EfEntityLookup(IHmmDataContext dataContext)
        {
            Guard.Against<ArgumentNullException>(dataContext == null, nameof(dataContext));
            _dataContext = dataContext;
        }

        public T GetEntity<T>(Guid id) where T : GuidEntity
        {
            T entity;
            if (typeof(T) == typeof(Author))
            {
                entity = _dataContext.Authors.Find(id) as T;
            }
            else
            {
                throw new DataSourceException($"{typeof(T)} is not support");
            }

            return entity;
        }

        public T GetEntity<T>(int id) where T : Entity
        {
            T entity;
            if (typeof(T) == typeof(NoteRender))
            {
                entity = _dataContext.Renders.Find(id) as T;
            }
            else if (typeof(T) == typeof(HmmNote))
            {
                entity = _dataContext.Notes.Find(id) as T;
            }
            else if (typeof(T) == typeof(NoteCatalog))
            {
                entity = _dataContext.Catalogs.Find(id) as T;
            }
            else if (typeof(T) == typeof(Subsystem))
            {
                entity = _dataContext.Subsystems.Find(id) as T;
            }
            else
            {
                throw new DataSourceException($"{typeof(T)} is not support");
            }

            return entity;
        }

        public PageList<T> GetEntities<T>(Expression<Func<T, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var (pageIdx, pageSize) = resourceCollectionParameters.GetPaginationTuple();
            var entities = GetQueryableEntities<T>();

            var result = query == null ? PageList<T>.Create(entities, pageIdx, pageSize) : PageList<T>.Create(entities.Where(query), pageIdx, pageSize);
            return result;
        }

        public async Task<PageList<T>> GetEntitiesAsync<T>(Expression<Func<T, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var (pageIdx, pageSize) = resourceCollectionParameters.GetPaginationTuple();
            var entities = GetQueryableEntities<T>();

            if (query != null)
            {
                var count = await entities.Where(query).CountAsync();
                entities = count <= pageSize ? entities.Where(query)
                    : entities.Where(query).Skip((pageIdx - 1) * pageSize).Take(pageSize).Cast<T>();
            }

            var result = await entities.ToListAsync();

            return result;
        }

        public async Task<T> GetEntityAsync<T>(int id) where T : Entity
        {
            T entity;
            if (typeof(T) == typeof(NoteRender))
            {
                entity = await _dataContext.Renders.FindAsync(id) as T;
            }
            else if (typeof(T) == typeof(HmmNote))
            {
                entity = await _dataContext.Notes.FindAsync(id) as T;
            }
            else if (typeof(T) == typeof(NoteCatalog))
            {
                entity = await _dataContext.Catalogs.FindAsync(id) as T;
            }
            else if (typeof(T) == typeof(Subsystem))
            {
                entity = await _dataContext.Subsystems.FindAsync(id) as T;
            }
            else
            {
                throw new DataSourceException($"{typeof(T)} is not support");
            }

            return entity;
        }

        public async Task<T> GetEntityAsync<T>(Guid id) where T : GuidEntity
        {
            T entity;
            if (typeof(T) == typeof(Author))
            {
                entity = await _dataContext.Authors.FindAsync(id) as T;
            }
            else
            {
                throw new DataSourceException($"{typeof(T)} is not support");
            }

            return entity;
        }

        public ProcessingResult ProcessResult { get; } = new();

        private IQueryable<T> GetQueryableEntities<T>()
        {
            IQueryable<T> entities;
            if (typeof(T) == typeof(NoteRender))
            {
                entities = _dataContext.Renders.Cast<T>();
            }
            else if (typeof(T) == typeof(HmmNote))
            {
                entities = _dataContext.Notes
                    .Include(n => n.Author)
                    .Include(n => n.Catalog).Cast<T>();
            }
            else if (typeof(T) == typeof(NoteCatalog))
            {
                entities = _dataContext.Catalogs
                    .Include(c => c.Subsystem)
                    .Include(c => c.Render).Cast<T>();
            }
            else if (typeof(T) == typeof(Author))
            {
                entities = _dataContext.Authors
                    .Cast<T>();
            }
            else if (typeof(T) == typeof(Subsystem))
            {
                entities = _dataContext.Subsystems
                    .Include(s => s.DefaultAuthor)
                    .Include(s => s.NoteCatalogs)
                    .ThenInclude(c => c.Render)
                    .Cast<T>();
            }
            else
            {
                throw new DataSourceException($"{typeof(T)} is not support");
            }

            return entities;
        }
    }
}