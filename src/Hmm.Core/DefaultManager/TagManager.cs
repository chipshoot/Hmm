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
        private readonly ValidatorBase<Tag> _validator;
        private readonly IEntityLookup _lookup;

        public TagManager(ICompositeEntityRepository<TagDao, HmmNoteDao> tagRepository, IMapper mapper, IEntityLookup lookup)
        {
            Guard.Against<ArgumentNullException>(tagRepository == null, nameof(tagRepository));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));
            _tagRepository = tagRepository;
            _mapper = mapper;
            _validator = new TagValidator(this);
            _lookup = lookup;
        }

        public async Task<PageList<Tag>> GetEntitiesAsync(Expression<Func<Tag, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
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
                return tags;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<Tag> GetTagByIdAsync(int id)
        {
            var tagDao = await _lookup.GetEntityAsync<TagDao>(id);
            if (tagDao == null)
            {
                return null;
            }

            switch (tagDao.IsActivated)
            {
                case false:
                    return null;

                default:
                    {
                        var tag = _mapper.Map<Tag>(tagDao);
                        switch (tag)
                        {
                            case null:
                                ProcessResult.AddErrorMessage("Cannot convert TagDao to Tag");
                                return null;

                            default:
                                return tag;
                        }
                    }
            }
        }

        public async Task<Tag> GetTagByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var tagName = name.Trim().ToLower();
            var tagDaos = await _lookup.GetEntitiesAsync<TagDao>(t => t.Name.ToLower() == tagName);
            var tagDao = tagDaos.FirstOrDefault();
            if (tagDao == null)
            {
                return null;
            }

            switch (tagDao.IsActivated)
            {
                case false:
                    return null;

                default:
                    {
                        var tag = _mapper.Map<Tag>(tagDao);
                        switch (tag)
                        {
                            case null:
                                ProcessResult.AddErrorMessage("Cannot convert TagDao to Tag");
                                return null;

                            default:
                                return tag;
                        }
                    }
            }
        }

        public async Task<bool> IsTagExistsAsync(int id)
        {
            try
            {
                var tagDao = await _lookup.GetEntityAsync<TagDao>(id);
                return tagDao is { IsActivated: true };
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return false;
            }
        }

        public async Task<Tag> CreateAsync(Tag tag)
        {
            try
            {
                ProcessResult.Rest();
                var isValid = await _validator.IsValidEntityAsync(tag, ProcessResult);
                if (!isValid)
                {
                    return null;
                }
                var tagDao = _mapper.Map<TagDao>(tag);
                if (tagDao == null)
                {
                    ProcessResult.AddErrorMessage("Cannot convert Tag to TagDao");
                    return null;
                }

                var addedTagDao = await _tagRepository.AddAsync(tagDao);
                if (addedTagDao == null)
                {
                    ProcessResult.PropagandaResult(_tagRepository.ProcessMessage);
                    return null;
                }

                tag.Id = addedTagDao.Id;
                return tag;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<Tag> UpdateAsync(Tag tag)
        {
            try
            {
                ProcessResult.Rest();
                var isValid = await _validator.IsValidEntityAsync(tag, ProcessResult);
                if (!isValid)
                {
                    return null;
                }

                var tagDao = _mapper.Map<TagDao>(tag);
                if (tagDao == null)
                {
                    ProcessResult.AddErrorMessage("Cannot convert Tag to TagDao");
                    return null;
                }

                var savedTagDao = await _lookup.GetEntityAsync<TagDao>(tag.Id);
                if (savedTagDao == null)
                {
                    ProcessResult.AddErrorMessage($"Cannot found Tag: {tag.Name} to update.");
                    return null;
                }

                var updatedTagDao = await _tagRepository.UpdateAsync(tagDao);
                if (updatedTagDao == null)
                {
                    ProcessResult.PropagandaResult(_tagRepository.ProcessMessage);
                    return null;
                }

                var updatedTag = _mapper.Map<Tag>(updatedTagDao);
                if (updatedTag == null)
                {
                    ProcessResult.AddErrorMessage("Cannot convert TagDao to Tag");
                    return null;
                }

                return updatedTag;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task DeActivateAsync(int id)
        {
            var tag = await _lookup.GetEntityAsync<TagDao>(id);
            if (tag == null)
            {
                ProcessResult.AddErrorMessage($"Cannot find tag with id : {id}", true);
            }
            else if (tag.IsActivated)
            {
                try
                {
                    tag.IsActivated = false;
                    await _tagRepository.UpdateAsync(tag);
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