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
using System.Collections.Generic;
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
                // Use cached expression helper to combine query with IsActivated filter
                var daoQuery = ExpressionHelper.CombineWithIsActivated<Tag, TagDao>(query);

                var tagDaosResult = await _tagRepository.GetEntitiesAsync(daoQuery, resourceCollectionParameters);
                if (!tagDaosResult.Success)
                {
                    return ProcessingResult<PageList<Tag>>.Fail(tagDaosResult.ErrorMessage, tagDaosResult.ErrorType);
                }

                var tags = _mapper.Map<PageList<Tag>>(tagDaosResult.Value);
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

            if (tagDaoResult.IsNotFound)
            {
                return ProcessingResult<Tag>.EmptyOk($"Tag with ID {id} not found");

            }

            var tagDao = tagDaoResult.Value;
            if (!tagDao.IsActivated)
            {
                return ProcessingResult<Tag>.Deleted($"Tag with ID {id} has been deactivated");
            }

            return _mapper.MapWithNullCheck<TagDao, Tag>(tagDao);
        }

        public async Task<ProcessingResult<Dictionary<int, Tag>>> GetTagsByIdsAsync(IEnumerable<int> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return ProcessingResult<Dictionary<int, Tag>>.Ok(new Dictionary<int, Tag>());
                }

                var idList = ids.Distinct().ToList();
                var tagDaosResult = await _lookup.GetEntitiesAsync<TagDao>(t => idList.Contains(t.Id) && t.IsActivated);

                if (!tagDaosResult.Success)
                {
                    return ProcessingResult<Dictionary<int, Tag>>.Fail(tagDaosResult.ErrorMessage, tagDaosResult.ErrorType);
                }

                var result = new Dictionary<int, Tag>();
                if (tagDaosResult.Value != null)
                {
                    foreach (var tagDao in tagDaosResult.Value)
                    {
                        var tag = _mapper.Map<Tag>(tagDao);
                        if (tag != null)
                        {
                            result[tag.Id] = tag;
                        }
                    }
                }

                return ProcessingResult<Dictionary<int, Tag>>.Ok(result);
            }
            catch (Exception ex)
            {
                return ProcessingResult<Dictionary<int, Tag>>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<Dictionary<string, Tag>>> GetTagsByNamesAsync(IEnumerable<string> names)
        {
            try
            {
                if (names == null || !names.Any())
                {
                    return ProcessingResult<Dictionary<string, Tag>>.Ok(new Dictionary<string, Tag>());
                }

                var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => n.Trim().ToLower())
                    .Distinct()
                    .ToList();

                if (!nameList.Any())
                {
                    return ProcessingResult<Dictionary<string, Tag>>.Ok(new Dictionary<string, Tag>());
                }

                var tagDaosResult = await _lookup.GetEntitiesAsync<TagDao>(t => nameList.Contains(t.Name.ToLower()) && t.IsActivated);

                if (!tagDaosResult.Success)
                {
                    return ProcessingResult<Dictionary<string, Tag>>.Fail(tagDaosResult.ErrorMessage, tagDaosResult.ErrorType);
                }

                var result = new Dictionary<string, Tag>(StringComparer.OrdinalIgnoreCase);
                if (tagDaosResult.Value != null)
                {
                    foreach (var tagDao in tagDaosResult.Value)
                    {
                        var tag = _mapper.Map<Tag>(tagDao);
                        if (tag != null)
                        {
                            result[tag.Name.ToLower()] = tag;
                        }
                    }
                }

                return ProcessingResult<Dictionary<string, Tag>>.Ok(result);
            }
            catch (Exception ex)
            {
                return ProcessingResult<Dictionary<string, Tag>>.FromException(ex);
            }
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

            if(tagDaosResult.IsNotFound)
            {
                return ProcessingResult<Tag>.EmptyOk($"Tag with name '{name}' not found");
            }

            var tagDao = tagDaosResult.Value.FirstOrDefault();
            if (tagDao == null)
            {
                return ProcessingResult<Tag>.EmptyOk($"Tag with name '{name}' not found");
            }

            if (!tagDao.IsActivated)
            {
                return ProcessingResult<Tag>.Deleted($"Tag with name '{name}' has been deactivated");
            }

            return _mapper.MapWithNullCheck<TagDao, Tag>(tagDao);
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

                var tagDaoResult = _mapper.MapWithNullCheck<Tag, TagDao>(tag);
                if (!tagDaoResult.Success)
                {
                    return ProcessingResult<Tag>.Fail(tagDaoResult.ErrorMessage);
                }

                var addedTagDaoResult = await _tagRepository.AddAsync(tagDaoResult.Value);
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

                var tagDaoResult = _mapper.MapWithNullCheck<Tag, TagDao>(tag);
                if (!tagDaoResult.Success)
                {
                    return ProcessingResult<Tag>.Fail(tagDaoResult.ErrorMessage);
                }

                var updatedTagDaoResult = await _tagRepository.UpdateAsync(tagDaoResult.Value);
                if (!updatedTagDaoResult.Success)
                {
                    return ProcessingResult<Tag>.Fail(updatedTagDaoResult.ErrorMessage, updatedTagDaoResult.ErrorType);
                }

                return _mapper.MapWithNullCheck<TagDao, Tag>(updatedTagDaoResult.Value);
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