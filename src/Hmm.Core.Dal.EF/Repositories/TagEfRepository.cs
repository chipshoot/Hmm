// Ignore Spelling: Ef

using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF.Repositories
{
    public class TagEfRepository(
        IHmmDataContext dataContext,
        IEntityLookup lookupRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger logger = null)
        : RepositoryBase(dataContext, lookupRepository, dateTimeProvider, logger), ICompositeEntityRepository<TagDao, HmmNoteDao>
    {
        public async Task<PageList<TagDao>> GetEntitiesAsync(Expression<Func<TagDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var cats = await LookupRepository.GetEntitiesAsync(query, resourceCollectionParameters);
            return cats;
        }

        public async Task<PageList<HmmNoteDao>> GetNoteByTagAsync(int tagId, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            ProcessMessage.Rest();
            try
            {
                var tag = await DataContext.Tags.Include(t => t.Notes).FirstOrDefaultAsync(t => t.Id == tagId);

                PageList<HmmNoteDao> notePage = null;
                if (tag != null)
                {
                    if (resourceCollectionParameters != null)
                    {
                        var noteList = tag.Notes
                            .Skip((resourceCollectionParameters.PageNumber - 1) * resourceCollectionParameters.PageSize)
                            .Take(resourceCollectionParameters.PageSize)
                            .Select(r => r.Note).ToList();
                        notePage = new PageList<HmmNoteDao>(noteList, tag.Notes.Count(), resourceCollectionParameters.PageNumber,
                            resourceCollectionParameters.PageSize);
                    }
                    else
                    {
                        var noteList = tag.Notes.Select(r => r.Note).ToList();
                        notePage = new PageList<HmmNoteDao>(noteList, 1, 1, tag.Notes.Count());
                    }
                }

                return notePage;
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public async Task<TagDao> GetEntityAsync(int id)
        {
            try
            {
                ProcessMessage.Rest();
                var tag = await DataContext.Tags
                    .FirstOrDefaultAsync(t => t.Id == id);
                return tag;
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public async Task<TagDao> AddAsync(TagDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Tags.Add(entity);
                await DataContext.SaveAsync();
                return entity;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<TagDao> UpdateAsync(TagDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();

            // ReSharper disable once PossibleNullReferenceException
            if (entity.Id <= 0)
            {
                ProcessMessage.Success = false;
                ProcessMessage.AddErrorMessage($"Can not update Tag with id {entity.Id}", true);
                return null;
            }

            try
            {
                DataContext.Tags.Update(entity);
                await DataContext.SaveAsync();
                var newTag = await LookupRepository.GetEntityAsync<TagDao>(entity.Id);
                return newTag;
            }
            catch (Exception ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<bool> DeleteAsync(TagDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();
            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                DataContext.Tags.Remove(entity);
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