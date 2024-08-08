using AutoMapper;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.Map;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.DefaultManager
{
    public class AuthorManager : IAuthorManager
    {
        private readonly IRepository<AuthorDao> _authorRepository;
        private readonly ValidatorBase<Author> _validator;
        private readonly IMapper _mapper;

        public AuthorManager(IRepository<AuthorDao> authorRepository, IMapper mapper)
        {
            Guard.Against<ArgumentNullException>(authorRepository == null, nameof(authorRepository));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));
            _authorRepository = authorRepository;
            _mapper = mapper;
            _validator = new AuthorValidator(this);
        }

        public async Task<Author> CreateAsync(Author authorInfo)
        {
            try
            {
                ProcessResult.Rest();
                if (!await _validator.IsValidEntityAsync(authorInfo, ProcessResult))
                {
                    return null;
                }

                var userDao = _mapper.Map<AuthorDao>(authorInfo);
                if (userDao == null)
                {
                    ProcessResult.AddErrorMessage("Cannot convert Author to AuthorDao");
                    return null;
                }

                var addedUsrDao = await _authorRepository.AddAsync(userDao);
                if (addedUsrDao == null)
                {
                    ProcessResult.PropagandaResult(_authorRepository.ProcessMessage);
                    return null;
                }

                authorInfo.Id = addedUsrDao.Id;
                return authorInfo;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<bool> AuthorExistsAsync(int id)
        {
            try
            {
                ProcessResult.Rest();
                var authorDao = await _authorRepository.GetEntityAsync(id);
                return authorDao != null;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return false;
            }
        }

        public async Task<Author> GetAuthorByIdAsync(int id)
        {
            ProcessResult.Rest();
            var authorDao = await _authorRepository.GetEntityAsync(id);
            if (authorDao == null)
            {
                return null;
            }

            var author = _mapper.Map<Author>(authorDao);
            if (author == null)
            {
                ProcessResult.AddErrorMessage("Cannot convert AuthorDao to Author");
                return null;
            }

            return author;
        }

        public async Task<Author> UpdateAsync(Author authorInfo)
        {
            try
            {
                ProcessResult.Rest();
                if (!await _validator.IsValidEntityAsync(authorInfo, ProcessResult))
                {
                    return null;
                }

                var authorDao = _mapper.Map<AuthorDao>(authorInfo);
                if (authorDao == null)
                {
                    ProcessResult.AddErrorMessage("Cannot convert Author to AuthorDao");
                    return null;
                }

                var savedAuthorDao = await _authorRepository.GetEntityAsync(authorInfo.Id);
                if (savedAuthorDao == null)
                {
                    ProcessResult.AddErrorMessage($"Cannot update author: {authorInfo.AccountName}, because system cannot find it in data source");
                    return null;
                }

                var updatedUserDao = await _authorRepository.UpdateAsync(authorDao);
                if (updatedUserDao == null)
                {
                    ProcessResult.PropagandaResult(_authorRepository.ProcessMessage);
                    return null;
                }

                var updatedUser = _mapper.Map<Author>(updatedUserDao);
                if (updatedUser == null)
                {
                    ProcessResult.AddErrorMessage("Cannot convert AuthorDao to Author");
                    return null;
                }

                return updatedUser;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<PageList<Author>> GetEntitiesAsync(Expression<Func<Author, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                ProcessResult.Rest();
                Expression<Func<AuthorDao, bool>> daoQuery = null;
                if (query != null)
                {
                    daoQuery = ExpressionMapper<Author, AuthorDao>.MapExpression(query);
                }

                var authorDaos = await _authorRepository.GetEntitiesAsync(daoQuery, resourceCollectionParameters);
                var authors = _mapper.Map<PageList<Author>>(authorDaos);
                return authors;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task DeActivateAsync(int id)
        {
            ProcessResult.Rest();
            var user = await _authorRepository.GetEntityAsync(id);
            if (user == null)
            {
                ProcessResult.AddErrorMessage($"Cannot find user with id : {id}", true);
            }
            else if (user.IsActivated)
            {
                try
                {
                    user.IsActivated = false;
                    await _authorRepository.UpdateAsync(user);
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