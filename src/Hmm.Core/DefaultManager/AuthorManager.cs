using AutoMapper;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHmmValidator<Author> _validator;
        private readonly IMapper _mapper;
        private readonly IEntityLookup _lookup;

        public AuthorManager(
            IRepository<AuthorDao> authorRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IEntityLookup lookup,
            IHmmValidator<Author> validator)
        {
            ArgumentNullException.ThrowIfNull(authorRepository);
            ArgumentNullException.ThrowIfNull(unitOfWork);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(lookup);
            ArgumentNullException.ThrowIfNull(validator);

            _authorRepository = authorRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _validator = validator;
            _lookup = lookup;
        }

        public async Task<ProcessingResult<PageList<Author>>> GetEntitiesAsync(Expression<Func<Author, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
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

                var authorDaosResult = await _authorRepository.GetEntitiesAsync(daoQuery, resourceCollectionParameters);
                if (!authorDaosResult.Success)
                {
                    return ProcessingResult<PageList<Author>>.Fail(authorDaosResult.ErrorMessage, authorDaosResult.ErrorType);
                }

                var authors = _mapper.Map<PageList<Author>>(authorDaosResult.Value);
                return ProcessingResult<PageList<Author>>.Ok(authors);
            }
            catch (Exception ex)
            {
                return ProcessingResult<PageList<Author>>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<Author>> GetAuthorByIdAsync(int id)
        {
            var authorDaoResult = await _lookup.GetEntityAsync<AuthorDao>(id);

            if (!authorDaoResult.Success)
            {
                return ProcessingResult<Author>.Fail(authorDaoResult.ErrorMessage, authorDaoResult.ErrorType);
            }

            var authorDao = authorDaoResult.Value;
            if (!authorDao.IsActivated)
            {
                return ProcessingResult<Author>.Deleted($"Author with ID {id} has been deactivated");
            }

            var author = _mapper.Map<Author>(authorDao);
            if (author == null)
            {
                return ProcessingResult<Author>.Fail("Cannot convert AuthorDao to Author");
            }

            return ProcessingResult<Author>.Ok(author);
        }

        public async Task<bool> IsAuthorExistsAsync(int id)
        {
            try
            {
                var authorDaoResult = await _lookup.GetEntityAsync<AuthorDao>(id);
                return authorDaoResult.Success;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ProcessingResult<Author>> CreateAsync(Author authorInfo)
        {
            try
            {
                var validationResult = await _validator.ValidateEntityAsync(authorInfo);
                if (!validationResult.Success)
                {
                    return ProcessingResult<Author>.Invalid(validationResult.GetWholeMessage());
                }

                var userDao = _mapper.Map<AuthorDao>(authorInfo);
                if (userDao == null)
                {
                    return ProcessingResult<Author>.Fail("Cannot convert Author to AuthorDao");
                }

                var addedUsrDaoResult = await _authorRepository.AddAsync(userDao);
                if (!addedUsrDaoResult.Success)
                {
                    return ProcessingResult<Author>.Fail(addedUsrDaoResult.ErrorMessage, addedUsrDaoResult.ErrorType);
                }

                await _unitOfWork.CommitAsync();

                var createdAuthor = _mapper.Map<Author>(addedUsrDaoResult.Value);
                return ProcessingResult<Author>.Ok(createdAuthor);
            }
            catch (Exception ex)
            {
                return ProcessingResult<Author>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<Author>> UpdateAsync(Author authorInfo)
        {
            try
            {
                var validationResult = await _validator.ValidateEntityAsync(authorInfo);
                if (!validationResult.Success)
                {
                    return ProcessingResult<Author>.Invalid(validationResult.GetWholeMessage());
                }

                var authorDao = _mapper.Map<AuthorDao>(authorInfo);
                if (authorDao == null)
                {
                    return ProcessingResult<Author>.Fail("Cannot convert Author to AuthorDao");
                }

                var savedAuthorDaoResult = await _lookup.GetEntityAsync<AuthorDao>(authorInfo.Id);
                if (!savedAuthorDaoResult.Success)
                {
                    return ProcessingResult<Author>.NotFound($"Cannot update author: {authorInfo.AccountName}, because system cannot find it in data source");
                }

                var updatedUserDaoResult = await _authorRepository.UpdateAsync(authorDao);
                if (!updatedUserDaoResult.Success)
                {
                    return ProcessingResult<Author>.Fail(updatedUserDaoResult.ErrorMessage, updatedUserDaoResult.ErrorType);
                }

                await _unitOfWork.CommitAsync();

                var updatedUser = _mapper.Map<Author>(updatedUserDaoResult.Value);
                if (updatedUser == null)
                {
                    return ProcessingResult<Author>.Fail("Cannot convert AuthorDao to Author");
                }

                return ProcessingResult<Author>.Ok(updatedUser);
            }
            catch (Exception ex)
            {
                return ProcessingResult<Author>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<Unit>> DeActivateAsync(int id)
        {
            try
            {
                var userResult = await _lookup.GetEntityAsync<AuthorDao>(id);
                if (!userResult.Success)
                {
                    return ProcessingResult<Unit>.NotFound($"Cannot find user with id: {id}");
                }

                var user = userResult.Value;
                if (!user.IsActivated)
                {
                    return ProcessingResult<Unit>.Ok(Unit.Value, $"User with id {id} is already deactivated");
                }

                user.IsActivated = false;
                var updatedResult = await _authorRepository.UpdateAsync(user);

                if (!updatedResult.Success)
                {
                    return ProcessingResult<Unit>.Fail(updatedResult.ErrorMessage, updatedResult.ErrorType);
                }

                await _unitOfWork.CommitAsync();

                return ProcessingResult<Unit>.Ok(Unit.Value, $"User with id {id} has been deactivated");
            }
            catch (Exception ex)
            {
                return ProcessingResult<Unit>.FromException(ex);
            }
        }
    }
}