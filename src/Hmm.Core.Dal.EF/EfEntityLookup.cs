// Ignore Spelling: Ef

using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
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
            ArgumentNullException.ThrowIfNull(dataContext);
            _dataContext = dataContext;
        }

        public async Task<ProcessingResult<PageList<T>>> GetEntitiesAsync<T>(Expression<Func<T, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var (pageIdx, pageSize) = resourceCollectionParameters.GetPaginationTuple();
                var entities = GetQueryableEntities<T>();

                var result = query == null
                    ? await PageList<T>.CreateAsync(entities, pageIdx, pageSize)
                    : await PageList<T>.CreateAsync(entities.Where(query), pageIdx, pageSize);

                return ProcessingResult<PageList<T>>.Ok(result);
            }
            catch (Exception ex)
            {
                return ProcessingResult<PageList<T>>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<T>> GetEntityAsync<T>(int id) where T : Entity
        {
            try
            {
                T entity;
                if (typeof(T) == typeof(AuthorDao))
                {
                    entity = await _dataContext.Set<AuthorDao>()
                        .AsNoTracking()
                        .Include(a => a.ContactInfo)
                        .FirstOrDefaultAsync(a => a.Id == id) as T;
                }
                else if (typeof(T) == typeof(ContactDao))
                {
                    entity = await _dataContext.Set<ContactDao>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id) as T;
                }
                else if (typeof(T) == typeof(HmmNoteDao))
                {
                    entity = await _dataContext.Set<HmmNoteDao>()
                        .AsNoTracking()
                        .Include(n => n.Author)
                        .Include(n => n.Catalog)
                        .Include(n => n.Tags)
                        .FirstOrDefaultAsync(n => n.Id == id) as T;
                }
                else if (typeof(T) == typeof(NoteCatalogDao))
                {
                    entity = await _dataContext.Set<NoteCatalogDao>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id) as T;
                }
                else if (typeof(T) == typeof(TagDao))
                {
                    entity = await _dataContext.Set<TagDao>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == id) as T;
                }
                else
                {
                    return ProcessingResult<T>.Fail($"{typeof(T).Name} is not supported", ErrorCategory.ValidationError);
                }

                if (entity == null)
                {
                    return ProcessingResult<T>.EmptyOk($"{typeof(T).Name} with ID {id} not found");
                }

                return ProcessingResult<T>.Ok(entity);
            }
            catch (Exception ex)
            {
                return ProcessingResult<T>.FromException(ex);
            }
        }

        private IQueryable<T> GetQueryableEntities<T>()
        {
            IQueryable<T> entities;
            if (typeof(T) == typeof(HmmNoteDao))
            {
                entities = _dataContext.Set<HmmNoteDao>()
                    .Include(n => n.Author)
                    .Include(n => n.Tags)
                    .Include(n => n.Catalog).AsNoTracking().Cast<T>();
            }
            else if (typeof(T) == typeof(NoteCatalogDao))
            {
                entities = _dataContext.Set<NoteCatalogDao>().AsNoTracking().Cast<T>();
            }
            else if (typeof(T) == typeof(AuthorDao))
            {
                entities = _dataContext.Set<AuthorDao>()
                    .Include(a => a.ContactInfo).AsNoTracking().Cast<T>();
            }
            else if (typeof(T) == typeof(ContactDao))
            {
                entities = _dataContext.Set<ContactDao>().AsNoTracking().Cast<T>();
            }
            else if (typeof(T) == typeof(TagDao))
            {
                entities = _dataContext.Set<TagDao>().AsNoTracking().Cast<T>();
            }
            else
            {
                throw new DataSourceException($"{typeof(T)} is not support");
            }

            return entities;
        }
    }
}