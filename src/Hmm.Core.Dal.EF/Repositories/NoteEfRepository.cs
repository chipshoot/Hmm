// Ignore Spelling: Ef

using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Hmm.Core.Dal.EF.Repositories
{
    public class NoteEfRepository(
        IHmmDataContext dataContext,
        IEntityLookup lookupRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger logger = null)
        : RepositoryBase(dataContext, lookupRepository, dateTimeProvider, logger), IVersionRepository<HmmNoteDao>
    {
        public async Task<PageList<HmmNoteDao>> GetEntitiesAsync(Expression<Func<HmmNoteDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var (pageIdx, pageSize) = resourceCollectionParameters.GetPaginationTuple();
            var notes = query == null
                ? DataContext.Notes.Include(n => n.Author).Include(n => n.Catalog).Include(n=>n.Tags)
                : DataContext.Notes.Include(n => n.Author).Include(n => n.Catalog).Include(n=>n.Tags).Where(query);
            var result = resourceCollectionParameters == null
                ? await PageList<HmmNoteDao>.CreateAsync(notes, pageIdx, pageSize)
                : await PageList<HmmNoteDao>.CreateAsync(notes.ApplySort(resourceCollectionParameters.OrderBy), pageIdx, pageSize);

            return result;
        }

        public async Task<HmmNoteDao> GetEntityAsync(int id)
        {
            try
            {
                var note = await DataContext.Notes
                    .Include(n=>n.Tags)
                    .FirstOrDefaultAsync(n=>n.Id == id);
                return note;
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public async Task<HmmNoteDao> AddAsync(HmmNoteDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // check if we need apply default catalog
                // ReSharper disable once PossibleNullReferenceException
                var catalog = await PropertyCheckingAsync(entity.Catalog);
                entity.Catalog = catalog ?? throw new Exception("Cannot find default note catalog.");

                entity.CreateDate = DateTimeProvider.UtcNow;
                entity.LastModifiedDate = DateTimeProvider.UtcNow;
                var savedAuthor = await DataContext.Authors.FindAsync(entity.Author.Id);
                if (savedAuthor != null)
                {
                    entity.Author = savedAuthor;
                }

                var savedCat = await DataContext.Catalogs.FindAsync(entity.Catalog.Id);
                if (savedCat != null)
                {
                    entity.Catalog = savedCat;
                }

                // reset the id to 0 to avoid EF core update the record
                entity.Id = 0;
                DataContext.Notes.Add(entity);
                await DataContext.SaveAsync();
                return entity;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<HmmNoteDao> UpdateAsync(HmmNoteDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // check if we need apply default catalog
                // ReSharper disable once PossibleNullReferenceException
                var catalog = await PropertyCheckingAsync(entity.Catalog);
                entity.Catalog = catalog ?? throw new Exception("Cannot find default note catalog.");

                entity.LastModifiedDate = DateTimeProvider.UtcNow;
                DataContext.Notes.Update(entity);
                await DataContext.SaveAsync();
                var savedRec = await LookupRepository.GetEntityAsync<HmmNoteDao>(entity.Id);

                return savedRec;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<bool> DeleteAsync(HmmNoteDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Notes.Remove(entity);
                await DataContext.SaveAsync();
                return true;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return false;
            }
        }

        public bool HasPropertyChanged(HmmNoteDao note, string propertyName)
        {
            if (DataContext is not DbContext dbContext)
            {
                return false;
            }

            var result = (from entry in dbContext.Entry(note).Properties
                          where entry.Metadata.Name == propertyName.Trim()
                          select entry.IsModified).FirstOrDefault();
            return result;
        }

        public void Flush()
        {
            DataContext.Save();
        }
    }
}