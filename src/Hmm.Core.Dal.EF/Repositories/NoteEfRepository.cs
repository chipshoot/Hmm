using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF.Repositories
{
    public class NoteEfRepository : RepositoryBase, IVersionRepository<HmmNote>
    {
        public NoteEfRepository(
             IHmmDataContext dataContext,
             IEntityLookup lookupRepo,
             IDateTimeProvider dateTimeProvider) : base(dataContext, lookupRepo, dateTimeProvider)
        {
        }

        public IQueryable<HmmNote> GetEntities(Expression<Func<HmmNote, bool>> query = null)
        {
            var notes = query != null
                ? DataContext.Notes
                    .Include(n => n.Author)
                    .Include(n => n.Catalog)
                    .Where(query)
                : DataContext.Notes
                    .Include(n => n.Author)
                    .Include(n => n.Catalog);
            return notes;
        }

        public async Task<IEnumerable<HmmNote>> GetEntitiesAsync(Expression<Func<HmmNote, bool>> query = null)
        {
            var notes = await GetEntities(query).ToListAsync();
            return notes;
        }

        public HmmNote GetEntity(int id)
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

        public async Task<HmmNote> GetEntityAsync(int id)
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

        public HmmNote Add(HmmNote entity)
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

        public HmmNote Update(HmmNote entity)
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
                DataContext.Save();
                var savedRec = LookupRepo.GetEntity<HmmNote>(entity.Id);

                return savedRec;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public bool Delete(HmmNote entity)
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

        public async Task<HmmNote> AddAsync(HmmNote entity)
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
                await DataContext.SaveAsync();
                return entity;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<HmmNote> UpdateAsync(HmmNote entity)
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
                var savedRec = LookupRepo.GetEntity<HmmNote>(entity.Id);

                return savedRec;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<bool> DeleteAsync(HmmNote entity)
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

        public bool HasPropertyChanged(HmmNote note, string propertyName)
        {
            if (!(DataContext is DbContext dbContext))
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