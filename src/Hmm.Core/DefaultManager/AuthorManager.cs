using System;
using System.Collections.Generic;
using System.Linq;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;

namespace Hmm.Core.DefaultManager
{
    public class AuthorManager : IAuthorManager
    {
        private readonly IGuidRepository<Author> _authorRepo;
        private readonly AuthorValidator _validator;

        public AuthorManager(IGuidRepository<Author> authorRepo, AuthorValidator validator)
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

        public bool AuthorExists(string id)
        {
            Guard.Against<ArgumentNullException>(string.IsNullOrEmpty(id), nameof(id));

            if (!Guid.TryParse(id, out var userId))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            return GetEntities().Any(u => u.Id == userId);
        }

        public Author Update(Author authorInfo)
        {
            try
            {
                if (!_validator.IsValidEntity(authorInfo, ProcessResult))
                {
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

        public IEnumerable<Author> GetEntities()
        {
            try
            {
                var authors = _authorRepo.GetEntities();

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
            var user = _authorRepo.GetEntities().FirstOrDefault(u => u.Id == id && u.IsActivated);
            if (user == null)
            {
                ProcessResult.Success = false;
                ProcessResult.AddErrorMessage($"Cannot find user with id : {id}", true);
            }
            else
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

        public ProcessingResult ProcessResult { get; } = new ProcessingResult();
    }
}