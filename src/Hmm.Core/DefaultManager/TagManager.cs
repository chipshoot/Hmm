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
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.DefaultManager
{
    public class TagManager : ITagManager
    {
        private readonly ICompositeEntityRepository<TagDao, HmmNoteDao> _tagRepository;
        private readonly IMapper _mapper;
        private readonly IHmmValidator<Tag> _validator;
        private readonly IEntityLookup _lookup;

        public TagManager(ICompositeEntityRepository<TagDao, HmmNoteDao> tagRepository, IMapper mapper, IEntityLookup lookup, IHmmValidator<Tag> validator)
        {
            ArgumentNullException.ThrowIfNull(tagRepository);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(validator);
            _tagRepository = tagRepository;
            _mapper = mapper;
            _validator = validator;
            _lookup = lookup;
        }

        public async Task<ProcessingResult<PageList<Tag>>> GetEntitiesAsync(Expression<Func<Tag, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                Expression<Func<TagDao, bool>> isActivatedExpression = t => t.IsActivated;
                Expression<Func<TagDao, bool>> daoQuery;
                if (query != null)
                {
                    var mappedQuery = ExpressionMapper<Tag, TagDao>.MapExpression(query);

                    // Combine the mapped query with the IsActivated expression
                    var parameter = Expression.Parameter(typeof(TagDao), "t");
                    var body = Expression.AndAlso(
                        Expression.Invoke(mappedQuery, parameter),
                        Expression.Invoke(isActivatedExpression, parameter)
                    );
                    daoQuery = Expression.Lambda<Func<TagDao, bool>>(body, parameter);
                }
                else
                {
                    daoQuery = isActivatedExpression;
                }

                var tagDaos = await _tagRepository.GetEntitiesAsync(daoQuery, resourceCollectionParameters);
                var tags = _mapper.Map<PageList<Tag>>(tagDaos);
                return ProcessingResult<PageList<Tag>>.Ok(tags);
            }
            catch (Exception ex)
            {
                return ProcessingResult<PageList<Tag>>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<Tag>> GetTagByIdAsync(int id)
        {
            var tagDaoResult = await _lookup.GetEntityAsync<TagDao>(id);

            if (!tagDaoResult.Success)
            {
                return ProcessingResult<Tag>.Fail(tagDaoResult.ErrorMessage, tagDaoResult.ErrorType);
            }

            var tagDao = tagDaoResult.Value;
            if (!tagDao.IsActivated)
            {
                return ProcessingResult<Tag>.Deleted($"Tag with ID {id} has been deactivated");
            }

            var tag = _mapper.Map<Tag>(tagDao);
            if (tag == null)
            {
                return ProcessingResult<Tag>.Fail("Cannot convert TagDao to Tag");
            }

            return ProcessingResult<Tag>.Ok(tag);
        }

        public async Task<ProcessingResult<Tag>> GetTagByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return ProcessingResult<Tag>.Invalid("Tag name cannot be null or empty");
            }

            var tagName = name.Trim().ToLower();
            var tagDaosResult = await _lookup.GetEntitiesAsync<TagDao>(t => t.Name.ToLower() == tagName);

            if (!tagDaosResult.Success)
            {
                return ProcessingResult<Tag>.Fail(tagDaosResult.ErrorMessage, tagDaosResult.ErrorType);
            }

            var tagDao = tagDaosResult.Value.FirstOrDefault();
            if (tagDao == null)
            {
                return ProcessingResult<Tag>.NotFound($"Tag with name '{name}' not found");
            }

            if (!tagDao.IsActivated)
            {
                return ProcessingResult<Tag>.Deleted($"Tag with name '{name}' has been deactivated");
            }

            var tag = _mapper.Map<Tag>(tagDao);
            if (tag == null)
            {
                return ProcessingResult<Tag>.Fail("Cannot convert TagDao to Tag");
            }

            return ProcessingResult<Tag>.Ok(tag);
        }

        public async Task<bool> IsTagExistsAsync(int id)
        {
            try
            {
                var tagDaoResult = await _lookup.GetEntityAsync<TagDao>(id);
                return tagDaoResult.Success && tagDaoResult.Value.IsActivated;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ProcessingResult<Tag>> CreateAsync(Tag tag)
        {
            try
            {
                var validationResult = await _validator.ValidateEntityAsync(tag);
                if (!validationResult.Success)
                {
                    return ProcessingResult<Tag>.Invalid(validationResult.GetWholeMessage());
                }

                var tagDao = _mapper.Map<TagDao>(tag);
                if (tagDao == null)
                {
                    return ProcessingResult<Tag>.Fail("Cannot convert Tag to TagDao");
                }

                var addedTagDaoResult = await _tagRepository.AddAsync(tagDao);
                if (!addedTagDaoResult.Success)
                {
                    return ProcessingResult<Tag>.Fail(addedTagDaoResult.ErrorMessage, addedTagDaoResult.ErrorType);
                }

                var createdTag = _mapper.Map<Tag>(addedTagDaoResult.Value);
                return ProcessingResult<Tag>.Ok(createdTag);
            }
            catch (Exception ex)
            {
                return ProcessingResult<Tag>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<Tag>> UpdateAsync(Tag tag)
        {
            try
            {
                var validationResult = await _validator.ValidateEntityAsync(tag);
                if (!validationResult.Success)
                {
                    return ProcessingResult<Tag>.Invalid(validationResult.GetWholeMessage());
                }

                var tagDao = _mapper.Map<TagDao>(tag);
                if (tagDao == null)
                {
                    return ProcessingResult<Tag>.Fail("Cannot convert Tag to TagDao");
                }

                var savedTagResult = await _lookup.GetEntityAsync<TagDao>(tag.Id);
                if (!savedTagResult.Success)
                {
                    return ProcessingResult<Tag>.NotFound($"Cannot update tag: {tag.Name}, because system cannot find it in data source");
                }

                var updatedTagDaoResult = await _tagRepository.UpdateAsync(tagDao);
                if (!updatedTagDaoResult.Success)
                {
                    return ProcessingResult<Tag>.Fail(updatedTagDaoResult.ErrorMessage, updatedTagDaoResult.ErrorType);
                }

                var updatedTag = _mapper.Map<Tag>(updatedTagDaoResult.Value);
                if (updatedTag == null)
                {
                    return ProcessingResult<Tag>.Fail("Cannot convert TagDao to Tag");
                }

                return ProcessingResult<Tag>.Ok(updatedTag);
            }
            catch (Exception ex)
            {
                return ProcessingResult<Tag>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<Unit>> DeActivateAsync(int id)
        {
            try
            {
                var tagResult = await _lookup.GetEntityAsync<TagDao>(id);
                if (!tagResult.Success)
                {
                    return ProcessingResult<Unit>.NotFound($"Cannot find tag with id: {id}");
                }

                var tag = tagResult.Value;
                if (!tag.IsActivated)
                {
                    return ProcessingResult<Unit>.Ok(Unit.Value, $"Tag with id {id} is already deactivated");
                }

                tag.IsActivated = false;
                var updatedResult = await _tagRepository.UpdateAsync(tag);

                if (!updatedResult.Success)
                {
                    return ProcessingResult<Unit>.Fail("Failed to deactivate tag");
                }

                return ProcessingResult<Unit>.Ok(Unit.Value, $"Tag with id {id} has been deactivated");
            }
            catch (Exception ex)
            {
                return ProcessingResult<Unit>.FromException(ex);
            }
        }
    }
}