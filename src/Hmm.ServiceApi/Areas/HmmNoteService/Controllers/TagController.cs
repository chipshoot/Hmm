using AutoMapper;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
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
            ArgumentNullException.ThrowIfNull(tagManager);
            ArgumentNullException.ThrowIfNull(mapper);

            _tagManager = tagManager;
            _mapper = mapper;
        }

        #endregion constructor

        [HttpGet(Name = "GetTags")]
        [TagsResultFilter]
        [CollectionResultFilter]
        public async Task<IActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var tagsResult = await _tagManager.GetEntitiesAsync(null, resourceCollectionParameters);
            if (!tagsResult.Success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, tagsResult.ErrorMessage);
            }

            if (tagsResult.Value == null || !tagsResult.Value.Any())
            {
                return NotFound();
            }

            return Ok(tagsResult.Value);
        }

        [HttpGet("{id:int}", Name = "GetTagById")]
        [TagResultFilter]
        public async Task<IActionResult> Get(int id)
        {
            var tagResult = await _tagManager.GetTagByIdAsync(id);
            if (!tagResult.Success)
            {
                if (tagResult.IsNotFound)
                {
                    return NotFound($"The tag : {id} not found.");
                }
                return StatusCode(StatusCodes.Status500InternalServerError, tagResult.ErrorMessage);
            }

            return Ok(tagResult.Value);
        }

        [HttpGet("{name}", Name = "GetTagByName")]
        [TagResultFilter]
        public async Task<IActionResult> Get(string name)
        {
            var tagResult = await _tagManager.GetTagByNameAsync(name);
            if (!tagResult.Success)
            {
                if (tagResult.IsNotFound)
                {
                    return NotFound($"The tag : {name} not found.");
                }
                return StatusCode(StatusCodes.Status500InternalServerError, tagResult.ErrorMessage);
            }

            return Ok(tagResult.Value);
        }

        // POST api/tags
        [HttpPost(Name = "AddTag")]
        [TagResultFilter]
        public async Task<IActionResult> Post([FromBody] ApiTagForCreate apiTag)
        {
            try
            {
                var tag = _mapper.Map<ApiTagForCreate, Tag>(apiTag);
                var newTagResult = await _tagManager.CreateAsync(tag);

                if (!newTagResult.Success)
                {
                    if (newTagResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(new ApiBadRequestResponse(newTagResult.ErrorMessage));
                    }
                    return StatusCode(StatusCodes.Status500InternalServerError, newTagResult.ErrorMessage);
                }

                return Created("", newTagResult.Value);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
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
                var curTagResult = await _tagManager.GetTagByIdAsync(id);
                if (!curTagResult.Success)
                {
                    if (curTagResult.IsNotFound)
                    {
                        return NotFound($"Tag with id {id} not found");
                    }
                    return StatusCode(StatusCodes.Status500InternalServerError, curTagResult.ErrorMessage);
                }

                var curTag = _mapper.Map(tag, curTagResult.Value);
                var updateResult = await _tagManager.UpdateAsync(curTag);

                if (!updateResult.Success)
                {
                    if (updateResult.IsNotFound)
                    {
                        return NotFound($"Tag with id {id} not found");
                    }
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
                    }
                    return StatusCode(StatusCodes.Status500InternalServerError, updateResult.ErrorMessage);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
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
                var curTagResult = await _tagManager.GetTagByIdAsync(id);
                if (!curTagResult.Success)
                {
                    if (curTagResult.IsNotFound)
                    {
                        return NotFound($"Tag with id {id} not found");
                    }
                    return StatusCode(StatusCodes.Status500InternalServerError, curTagResult.ErrorMessage);
                }

                var tag2Update = _mapper.Map<ApiTagForUpdate>(curTagResult.Value);
                patchDoc.ApplyTo(tag2Update);
                _mapper.Map(tag2Update, curTagResult.Value);

                var updateResult = await _tagManager.UpdateAsync(curTagResult.Value);
                if (!updateResult.Success)
                {
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
                    }
                    return StatusCode(StatusCodes.Status500InternalServerError, updateResult.ErrorMessage);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // DELETE api/tags/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deleteResult = await _tagManager.DeActivateAsync(id);

                if (!deleteResult.Success)
                {
                    if (deleteResult.IsNotFound)
                    {
                        return NotFound($"Tag with id {id} not found");
                    }
                    return StatusCode(StatusCodes.Status500InternalServerError, deleteResult.ErrorMessage);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}