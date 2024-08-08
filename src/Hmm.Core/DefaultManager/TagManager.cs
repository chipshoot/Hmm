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

        public TagManager(ICompositeEntityRepository<TagDao, HmmNoteDao> tagRepository, IMapper mapper)
        {
            Guard.Against<ArgumentNullException>(tagRepository == null, nameof(tagRepository));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));
            _tagRepository = tagRepository;
            _mapper = mapper;
            _validator = new TagValidator(this);
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

        public async Task<bool> TagExistsAsync(int id)
        {
            try
            {
                var tagDao = await _tagRepository.GetEntityAsync(id);
                return tagDao != null;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return false;
            }
        }

        public async Task<Tag> GetTagByIdAsync(int id)
        {
            var tagDao = await _tagRepository.GetEntityAsync(id);
            if (tagDao == null)
            {
                return null;
            }

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

        public async Task<Tag> GetTagByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var tagDaos = await _tagRepository.GetEntitiesAsync(t=>t.Name == name);
            var tagDao = tagDaos.FirstOrDefault();
            if (tagDao == null)
            {
                return null;
            }

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

                var savedTagDao = await _tagRepository.GetEntityAsync(tag.Id);
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

        public async Task<PageList<Tag>> GetEntitiesAsync(Expression<Func<Tag, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                Expression<Func<TagDao, bool>> daoQuery = null;
                if (query != null)
                {
                    daoQuery = ExpressionMapper<Tag, TagDao>.MapExpression(query);
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

        public async Task DeActivateAsync(int id)
        {
            var tag = await _tagRepository.GetEntityAsync(id);
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