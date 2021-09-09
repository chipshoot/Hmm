using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Linq.Expressions;

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

        public IQueryable<Author> GetEntities(Expression<Func<Author, bool>> query = null)
        {
            return _lookupRepo.GetEntities(query);
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

        public void Flush()
        {
            _dataContext.Save();
        }

        public ProcessingResult ProcessMessage { get; } = new ();
    }
}