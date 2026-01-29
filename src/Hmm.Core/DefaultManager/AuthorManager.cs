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
                // Use cached expression helper to combine query with IsActivated filter
                var daoQuery = ExpressionHelper.CombineWithIsActivated<Author, AuthorDao>(query);

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

            return _mapper.MapWithNullCheck<AuthorDao, Author>(authorDao);
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

                var userDaoResult = _mapper.MapWithNullCheck<Author, AuthorDao>(authorInfo);
                if (!userDaoResult.Success)
                {
                    return ProcessingResult<Author>.Fail(userDaoResult.ErrorMessage);
                }

                var addedUsrDaoResult = await _authorRepository.AddAsync(userDaoResult.Value);
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

                var authorDaoResult = _mapper.MapWithNullCheck<Author, AuthorDao>(authorInfo);
                if (!authorDaoResult.Success)
                {
                    return ProcessingResult<Author>.Fail(authorDaoResult.ErrorMessage);
                }

                var updatedUserDaoResult = await _authorRepository.UpdateAsync(authorDaoResult.Value);
                if (!updatedUserDaoResult.Success)
                {
                    return ProcessingResult<Author>.Fail(updatedUserDaoResult.ErrorMessage, updatedUserDaoResult.ErrorType);
                }

                await _unitOfWork.CommitAsync();

                return _mapper.MapWithNullCheck<AuthorDao, Author>(updatedUserDaoResult.Value);
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