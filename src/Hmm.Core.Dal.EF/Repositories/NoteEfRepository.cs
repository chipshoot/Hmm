// Ignore Spelling: Ef

using Hmm.Core.Dal.EF.DbEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF.Repositories
{
    public class NoteEfRepository(
        IHmmDataContext dataContext,
        IEntityLookup lookupRepository,
        IDateTimeProvider dateTimeProvider)
        : RepositoryBase(dataContext, lookupRepository, dateTimeProvider), IVersionRepository<HmmNoteDao>
    {
        public PageList<HmmNoteDao> GetEntities(Expression<Func<HmmNoteDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var (pageIdx, pageSize) = resourceCollectionParameters.GetPaginationTuple();
            var notes = query == null
                ? DataContext.Notes.Include(n => n.Author).Include(n => n.Catalog)
                : DataContext.Notes.Include(n => n.Author).Include(n => n.Catalog).Where(query);

            var result = resourceCollectionParameters == null
                ? PageList<HmmNoteDao>.Create(notes, pageIdx, pageSize)
                : PageList<HmmNoteDao>.Create(notes.ApplySort(resourceCollectionParameters.OrderBy), pageIdx, pageSize);

            return result;
        }

        public async Task<PageList<HmmNoteDao>> GetEntitiesAsync(Expression<Func<HmmNoteDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var (pageIdx, pageSize) = resourceCollectionParameters.GetPaginationTuple();
            var notes = query == null
                ? DataContext.Notes.Include(n => n.Author).Include(n => n.Catalog)
                : DataContext.Notes.Include(n => n.Author).Include(n => n.Catalog).Where(query);
            var result = resourceCollectionParameters == null
                ? await PageList<HmmNoteDao>.CreateAsync(notes, pageIdx, pageSize)
                : await PageList<HmmNoteDao>.CreateAsync(notes.ApplySort(resourceCollectionParameters.OrderBy), pageIdx, pageSize);

            return result;
        }

        public HmmNoteDao GetEntity(int id)
        {
            try
            {
                return DataContext.Notes.Find(id);
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public async Task<HmmNoteDao> GetEntityAsync(int id)
        {
            try
            {
                var note = await DataContext.Notes.FindAsync(id);
                return note;
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public HmmNoteDao Add(HmmNoteDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // Check if the note's catalog is specified. If not, attempt to apply a default catalog.
                // ReSharper disable once PossibleNullReferenceException
                var catalog = PropertyChecking(entity.Catalog);
                entity.Catalog = catalog ?? throw new Exception("Cannot find default note catalog.");

                entity.CreateDate = DateTimeProvider.UtcNow;
                entity.LastModifiedDate = DateTimeProvider.UtcNow;
                var savedAuthor = DataContext.Authors.Find(entity.Author.Id);
                if (savedAuthor != null)
                {
                    entity.Author = savedAuthor;
                }

                var savedCat = DataContext.Catalogs.Find(entity.Catalog.Id);
                if (savedCat != null)
                {
                    entity.Catalog = savedCat;
                }
                DataContext.Notes.Add(entity);
                DataContext.Save();
                return entity;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public HmmNoteDao Update(HmmNoteDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // Check if the note's catalog is specified. If not, attempt to apply a default catalog.
                // ReSharper disable once PossibleNullReferenceException
                var catalog = PropertyChecking(entity.Catalog);
                entity.Catalog = catalog ?? throw new Exception("Cannot find default note catalog.");

                entity.LastModifiedDate = DateTimeProvider.UtcNow;
                DataContext.Notes.Update(entity);
                DataContext.Save();
                var savedRec = LookupRepository.GetEntity<HmmNoteDao>(entity.Id);

                return savedRec;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public bool Delete(HmmNoteDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Notes.Remove(entity);
                DataContext.Save();
                return true;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return false;
            }
        }

        public async Task<HmmNoteDao> AddAsync(HmmNoteDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // check if need apply default catalog
                // ReSharper disable once PossibleNullReferenceException
                var catalog = PropertyChecking(entity.Catalog);
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
                // check if need apply default catalog
                // ReSharper disable once PossibleNullReferenceException
                var catalog = PropertyChecking(entity.Catalog);
                entity.Catalog = catalog ?? throw new Exception("Cannot find default note catalog.");

                entity.LastModifiedDate = DateTimeProvider.UtcNow;
                DataContext.Notes.Update(entity);
                await DataContext.SaveAsync();
                var savedRec = LookupRepository.GetEntity<HmmNoteDao>(entity.Id);

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