using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF.Repositories
{
    public class AuthorEfRepository : IGuidRepository<Author>
    {
        private readonly IHmmDataContext _dataContext;
        private readonly IEntityLookup _lookupRepo;

        public AuthorEfRepository(IHmmDataContext dataContext, IEntityLookup lookupRepo)
        {
            Guard.Against<ArgumentNullException>(dataContext == null, nameof(dataContext));
            Guard.Against<ArgumentNullException>(lookupRepo == null, nameof(lookupRepo));

            _dataContext = dataContext;
            _lookupRepo = lookupRepo;
        }

        public PageList<Author> GetEntities(Expression<Func<Author, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            return _lookupRepo.GetEntities(query, resourceCollectionParameters);
        }

        public async Task<PageList<Author>> GetEntitiesAsync(Expression<Func<Author, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var authors = await _lookupRepo.GetEntitiesAsync(query, resourceCollectionParameters);
            return authors;
        }

        public Author GetEntity(Guid id)
        {
            try
            {
                return _dataContext.Authors.Find(id);
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public async Task<Author> GetEntityAsync(Guid id)
        {
            try
            {
                var author = await _dataContext.Authors.FindAsync(id);
                return author;
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public Author Add(Author entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                _dataContext.Authors.Add(entity);
                Flush();
                return entity;
            }
            catch (DataSourceException ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public Author Update(Author entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // ReSharper disable once PossibleNullReferenceException
                if (entity.Id == Guid.Empty)
                {
                    ProcessMessage.Success = false;
                    ProcessMessage.AddErrorMessage($"Can not update author with id {entity.Id}");
                    return null;
                }

                _dataContext.Authors.Update(entity);
                Flush();
                var updateAuthor = _lookupRepo.GetEntity<Author>(entity.Id);
                return updateAuthor;
            }
            catch (DataSourceException ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public bool Delete(Author entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                _dataContext.Authors.Remove(entity);
                Flush();
                return true;
            }
            catch (DataSourceException ex)
            {
                ProcessMessage.WrapException(ex);
                return false;
            }
        }

        public async Task<Author> AddAsync(Author entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                _dataContext.Authors.Add(entity);
                await _dataContext.SaveAsync();
                return entity;
            }
            catch (DataSourceException ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<Author> UpdateAsync(Author entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // ReSharper disable once PossibleNullReferenceException
                if (entity.Id == Guid.Empty)
                {
                    ProcessMessage.Success = false;
                    ProcessMessage.AddErrorMessage($"Can not update author with id {entity.Id}");
                    return null;
                }

                _dataContext.Authors.Update(entity);
                await _dataContext.SaveAsync();
                var updateAuthor = await _lookupRepo.GetEntityAsync<Author>(entity.Id);
                return updateAuthor;
            }
            catch (DataSourceException ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<bool> DeleteAsync(Author entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                _dataContext.Authors.Remove(entity);
                await _dataContext.SaveAsync();
                return true;
            }
            catch (DataSourceException ex)
            {
                ProcessMessage.WrapException(ex);
                return false;
            }
        }

        public void Flush()
        {
            _dataContext.Save();
        }

        public ProcessingResult ProcessMessage { get; } = new();
    }
}