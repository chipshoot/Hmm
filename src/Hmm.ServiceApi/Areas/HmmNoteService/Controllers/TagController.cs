using AutoMapper;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Controllers
{
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/tags")]
    public class TagController : Controller
    {
        #region private fields

        private readonly ITagManager _tagManager;
        private readonly IMapper _mapper;

        #endregion private fields

        #region constructor

        public TagController(ITagManager tagManager, IMapper mapper)
        {
            Guard.Against<ArgumentNullException>(tagManager == null, nameof(tagManager));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));

            _tagManager = tagManager;
            _mapper = mapper;
        }

        #endregion constructor

        [HttpGet(Name = "GetTags")]
        [TagsResultFilter]
        [CollectionResultFilter]
        public async Task<IActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var tags = await _tagManager.GetEntitiesAsync(null, resourceCollectionParameters);
            if (!tags.Any())
            {
                return NotFound();
            }

            return Ok(tags);
        }

        [HttpGet("{id:int}", Name = "GetTagById")]
        [TagResultFilter]
        public async Task<IActionResult> Get(int id)
        {
            var tag = await _tagManager.GetTagByIdAsync(id);
            if (tag == null)
            {
                return NotFound($"The tag : {id} not found.");
            }

            return Ok(tag);
        }

        // POST api/tags
        [HttpPost(Name = "AddTag")]
        [TagResultFilter]
        public async Task<IActionResult> Post([FromBody] ApiTagForCreate apiTag)
        {
            try
            {
                var tag = _mapper.Map<ApiTagForCreate, Tag>(apiTag);
                var newTag = await _tagManager.CreateAsync(tag);

                if (newTag == null)
                {
                    return BadRequest(_tagManager.ProcessResult.GetWholeMessage());
                }

                return Created("", newTag);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT api/tags/5
        [HttpPut("{id:int}", Name = "UpdateTag")]
        public async Task<IActionResult> Put(int id, [FromBody] ApiTagForUpdate tag)
        {
            if (tag == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Tag information is null or invalid id found"));
            }

            try
            {
                var curTag = await _tagManager.GetTagByIdAsync(id);
                if (curTag == null)
                {
                    return BadRequest($"Tag {id} cannot be found.");
                }

                curTag = _mapper.Map(tag, curTag);
                var newTag = await _tagManager.UpdateAsync(curTag);
                if (newTag == null)
                {
                    return BadRequest(_tagManager.ProcessResult.MessageList);
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PATCH api/tags/5
        [HttpPatch("{id:int}", Name = "PatchTag")]
        public async Task<IActionResult> Patch(int id, [FromBody] JsonPatchDocument<ApiTagForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var curTag = await _tagManager.GetTagByIdAsync(id);
                if (curTag == null)
                {
                    return NotFound();
                }

                var tag2Update = _mapper.Map<ApiTagForUpdate>(curTag);
                patchDoc.ApplyTo(tag2Update);
                _mapper.Map(tag2Update, curTag);

                var newTag = await _tagManager.UpdateAsync(curTag);
                if (newTag == null)
                {
                    return BadRequest(_tagManager.ProcessResult.MessageList);
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // DELETE api/tags/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Invalid tag id found"));
            }

            try
            {
                var curTag = await _tagManager.GetTagByIdAsync(id);
                if (curTag == null)
                {
                    return BadRequest($"Tag {id} cannot be found.");
                }

                curTag.IsActivated = false;
                await _tagManager.DeActivateAsync(id);
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}