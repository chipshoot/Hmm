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
        private readonly IEntityLookup _lookup;

        public AuthorManager(IRepository<AuthorDao> authorRepository, IMapper mapper, IEntityLookup lookup)
        {
            Guard.Against<ArgumentNullException>(authorRepository == null, nameof(authorRepository));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));
            Guard.Against<ArgumentNullException>(lookup == null, nameof(lookup));

            _authorRepository = authorRepository;
            _mapper = mapper;
            _validator = new AuthorValidator(this);
            _lookup = lookup;
        }

        public async Task<PageList<Author>> GetEntitiesAsync(Expression<Func<Author, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                ProcessResult.Rest();
                Expression<Func<AuthorDao, bool>> isActivatedExpression = t => t.IsActivated;
                Expression<Func<AuthorDao, bool>> daoQuery = null;
                if (query != null)
                {
                    var mappedQuery = ExpressionMapper<Author, AuthorDao>.MapExpression(query);

                    // Combine the mapped query with the IsActivated expression
                    var parameter = Expression.Parameter(typeof(AuthorDao), "a");
                    var body = Expression.AndAlso(
                        Expression.Invoke(mappedQuery, parameter),
                        Expression.Invoke(isActivatedExpression, parameter)
                    );
                    daoQuery = Expression.Lambda<Func<AuthorDao, bool>>(body, parameter);
                }
                else
                {
                    daoQuery = isActivatedExpression;
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

        public async Task<Author> GetAuthorByIdAsync(int id)
        {
            ProcessResult.Rest();
            var authorDao = await _lookup.GetEntityAsync<AuthorDao>(id);
            switch (authorDao)
            {
                case null:
                    return null;

                case { IsActivated: false }:
                    return null;
            }

            var author = _mapper.Map<Author>(authorDao);
            switch (author)
            {
                case null:
                    ProcessResult.AddErrorMessage("Cannot convert AuthorDao to Author");
                    return null;

                default:
                    return author;
            }
        }

        public async Task<bool> IsAuthorExistsAsync(int id)
        {
            try
            {
                ProcessResult.Rest();
                var authorDao = await _lookup.GetEntityAsync<AuthorDao>(id);
                return authorDao != null;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return false;
            }
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

                authorInfo = _mapper.Map<Author>(addedUsrDao);
                return authorInfo;
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

                var savedAuthorDao = await _lookup.GetEntityAsync<AuthorDao>(authorInfo.Id);
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
                switch (updatedUser)
                {
                    case null:
                        ProcessResult.AddErrorMessage("Cannot convert AuthorDao to Author");
                        return null;

                    default:
                        return updatedUser;
                }
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
            switch (user)
            {
                case null:
                    ProcessResult.AddErrorMessage($"Cannot find user with id : {id}", true);
                    break;

                default:
                    {
                        if (user.IsActivated)
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

                        break;
                    }
            }
        }

        public ProcessingResult ProcessResult { get; } = new();
    }
}