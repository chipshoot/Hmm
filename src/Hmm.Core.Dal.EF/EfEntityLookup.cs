// Ignore Spelling: Ef

using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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

        public async Task<PageList<T>> GetEntitiesAsync<T>(Expression<Func<T, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var (pageIdx, pageSize) = resourceCollectionParameters.GetPaginationTuple();
            var entities = GetQueryableEntities<T>();

            var result = query == null
                ? await PageList<T>.CreateAsync(entities, pageIdx, pageSize)
                : await PageList<T>.CreateAsync(entities.Where(query), pageIdx, pageSize);

            return result;
        }

        public async Task<T> GetEntityAsync<T>(int id) where T : Entity
        {
            T entity;
            if (typeof(T) == typeof(AuthorDao))
            {
                entity = await _dataContext.Authors.FindAsync(id) as T;
            }
            else if (typeof(T) == typeof(ContactDao))
            {
                entity = await _dataContext.Contacts.FindAsync(id) as T;
            }
            else if (typeof(T) == typeof(HmmNoteDao))
            {
                entity = await _dataContext.Notes.FindAsync(id) as T;
            }
            else if (typeof(T) == typeof(NoteCatalogDao))
            {
                entity = await _dataContext.Catalogs.FindAsync(id) as T;
            }
            else if (typeof(T) == typeof(TagDao))
            {
                entity = await _dataContext.Tags.FindAsync(id) as T;
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
            if (typeof(T) == typeof(HmmNoteDao))
            {
                entities = _dataContext.Notes
                    .Include(n => n.Author)
                    .Include(n => n.Catalog).Cast<T>();
            }
            if (typeof(T) == typeof(NoteCatalogDao))
            {
                entities = _dataContext.Catalogs.Cast<T>();
            }
            else if (typeof(T) == typeof(AuthorDao))
            {
                entities = _dataContext.Authors.Cast<T>();
            }
            else if (typeof(T) == typeof(ContactDao))
            {
                entities = _dataContext.Contacts.Cast<T>();
            }
            else if (typeof(T) == typeof(TagDao))
            {
                entities = _dataContext.Tags.Cast<T>();
            }
            else
            {
                throw new DataSourceException($"{typeof(T)} is not support");
            }

            return entities;
        }
    }
}