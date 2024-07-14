// Ignore Spelling: Repo Ef

using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AuthorDao = Hmm.Core.Dal.EF.DbEntity.AuthorDao;

namespace Hmm.Core.Dal.EF.Repositories
{
    public class AuthorEfRepository : IRepository<AuthorDao>
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

        public PageList<AuthorDao> GetEntities(Expression<Func<AuthorDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            return _lookupRepo.GetEntities(query, resourceCollectionParameters);
        }

        public async Task<PageList<AuthorDao>> GetEntitiesAsync(Expression<Func<AuthorDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var authors = await _lookupRepo.GetEntitiesAsync(query, resourceCollectionParameters);
            return authors;
        }

        public AuthorDao GetEntity(int id)
        {
            try
            {
                ProcessMessage.Rest();
                return _dataContext.Authors.Find(id);
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public async Task<AuthorDao> GetEntityAsync(int id)
        {
            try
            {
                ProcessMessage.Rest();
                var author = await _dataContext.Authors.FindAsync(id);
                return author;
            }
            catch (Exception e)
            {
                ProcessMessage.WrapException(e);
                return null;
            }
        }

        public AuthorDao Add(AuthorDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();
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

        public AuthorDao Update(AuthorDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();
            try
            {
                // ReSharper disable once PossibleNullReferenceException
                if (entity.Id <= 0)
                {
                    ProcessMessage.Success = false;
                    ProcessMessage.AddErrorMessage($"Can not update author with id {entity.Id}");
                    return null;
                }

                _dataContext.Authors.Update(entity);
                Flush();
                var updateAuthor = _lookupRepo.GetEntity<AuthorDao>(entity.Id);
                return updateAuthor;
            }
            catch (DataSourceException ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public bool Delete(AuthorDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();
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

        public async Task<AuthorDao> AddAsync(AuthorDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();
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

        public async Task<AuthorDao> UpdateAsync(AuthorDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();
            try
            {
                // ReSharper disable once PossibleNullReferenceException
                if (entity.Id <= 0)
                {
                    ProcessMessage.Success = false;
                    ProcessMessage.AddErrorMessage($"Can not update author with id {entity.Id}");
                    return null;
                }

                _dataContext.Authors.Update(entity);
                await _dataContext.SaveAsync();
                var updateAuthor = await _lookupRepo.GetEntityAsync<AuthorDao>(entity.Id);
                return updateAuthor;
            }
            catch (DataSourceException ex)
            {
                ProcessMessage.WrapException(ex);
                return null;
            }
        }

        public async Task<bool> DeleteAsync(AuthorDao entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            ProcessMessage.Rest();
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