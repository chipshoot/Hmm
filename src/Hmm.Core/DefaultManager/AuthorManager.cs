using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.DefaultManager
{
    public class AuthorManager : IAuthorManager
    {
        private readonly IGuidRepository<Author> _authorRepo;
        private readonly IHmmValidator<Author> _validator;

        public AuthorManager(IGuidRepository<Author> authorRepo, IHmmValidator<Author> validator)
        {
            Guard.Against<ArgumentNullException>(authorRepo == null, nameof(authorRepo));
            Guard.Against<ArgumentNullException>(validator == null, nameof(validator));
            _authorRepo = authorRepo;
            _validator = validator;
        }

        public Author Create(Author authorInfo)
        {
            // reset user id to apply unique account name validation
            authorInfo.Id = Guid.Empty;
            if (!_validator.IsValidEntity(authorInfo, ProcessResult))
            {
                return null;
            }

            try
            {
                authorInfo.Id = Guid.NewGuid();
                var addedUsr = _authorRepo.Add(authorInfo);
                return addedUsr;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<Author> CreateAsync(Author authorInfo)
        {
            // reset user id to apply unique account name validation
            authorInfo.Id = Guid.Empty;
            if (!_validator.IsValidEntity(authorInfo, ProcessResult))
            {
                return null;
            }

            try
            {
                authorInfo.Id = Guid.NewGuid();
                var addedUsr = await _authorRepo.AddAsync(authorInfo);
                return addedUsr;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public bool AuthorExists(string id)
        {
            Guard.Against<ArgumentNullException>(string.IsNullOrEmpty(id), nameof(id));

            if (!Guid.TryParse(id, out var userId))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            return _authorRepo.GetEntity(userId) != null;
        }

        public Author GetAuthorById(Guid id)
        {
            var author = _authorRepo.GetEntity(id);
            if (author == null)
            {
                ProcessResult.PropagandaResult(_authorRepo.ProcessMessage);
            }

            return author;
        }

        public async Task<Author> GetAuthorByIdAsync(Guid id)
        {
            var author = await _authorRepo.GetEntityAsync(id);
            if (author == null)
            {
                ProcessResult.PropagandaResult(_authorRepo.ProcessMessage);
            }

            return author;
        }

        public async Task<bool> AuthorExistsAsync(string id)
        {
            Guard.Against<ArgumentNullException>(string.IsNullOrEmpty(id), nameof(id));

            if (!Guid.TryParse(id, out var userId))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            var author = await _authorRepo.GetEntityAsync(userId);
            return author != null;
        }

        public Author Update(Author authorInfo)
        {
            try
            {
                if (!_validator.IsValidEntity(authorInfo, ProcessResult))
                {
                    return null;
                }

                if (!AuthorExists(authorInfo.Id.ToString()))
                {
                    ProcessResult.AddErrorMessage($"Cannot update author: {authorInfo.AccountName}, because system cannot find it in data source");
                    return null;
                }
                var updatedUser = _authorRepo.Update(authorInfo);
                if (updatedUser == null)
                {
                    ProcessResult.PropagandaResult(_authorRepo.ProcessMessage);
                }

                return updatedUser;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<Author> UpdateAsync(Author authorInfo)
        {
            try
            {
                if (!_validator.IsValidEntity(authorInfo, ProcessResult))
                {
                    return null;
                }

                var authorExists = await AuthorExistsAsync(authorInfo.Id.ToString());
                if (!authorExists)
                {
                    ProcessResult.AddErrorMessage($"Cannot update author: {authorInfo.AccountName}, because system cannot find it in data source");
                    return null;
                }
                var updatedUser = await _authorRepo.UpdateAsync(authorInfo);
                if (updatedUser == null)
                {
                    ProcessResult.PropagandaResult(_authorRepo.ProcessMessage);
                }

                return updatedUser;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public IEnumerable<Author> GetEntities( Expression<Func<Author, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var authors = _authorRepo.GetEntities(query, resourceCollectionParameters);

                return authors;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<IEnumerable<Author>> GetEntitiesAsync(Expression<Func<Author, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var authors = await _authorRepo.GetEntitiesAsync(query, resourceCollectionParameters);
                return authors;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public void DeActivate(Guid id)
        {
            var user = _authorRepo.GetEntity(id);
            if (user == null)
            {
                ProcessResult.Success = false;
                ProcessResult.AddErrorMessage($"Cannot find user with id : {id}", true);
            }
            else if (user.IsActivated)
            {
                try
                {
                    user.IsActivated = false;
                    _authorRepo.Update(user);
                }
                catch (Exception ex)
                {
                    ProcessResult.WrapException(ex);
                }
            }
        }

        public async Task DeActivateAsync(Guid id)
        {
            var user = await _authorRepo.GetEntityAsync(id);
            if (user == null)
            {
                ProcessResult.Success = false;
                ProcessResult.AddErrorMessage($"Cannot find user with id : {id}", true);
            }
            else if (user.IsActivated)
            {
                try
                {
                    user.IsActivated = false;
                    await _authorRepo.UpdateAsync(user);
                }
                catch (Exception ex)
                {
                    ProcessResult.WrapException(ex);
                }
            }
        }

        public ProcessingResult ProcessResult { get; } = new();
    }
}