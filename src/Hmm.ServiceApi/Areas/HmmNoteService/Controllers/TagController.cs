using AutoMapper;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Controllers
{
    [Authorize]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/tags")]
    public class TagController : Controller
    {
        #region private fields

        private readonly ITagManager _tagManager;
        private readonly IMapper _mapper;
        private readonly ILogger<TagController> _logger;

        #endregion private fields

        #region constructor

        public TagController(ITagManager tagManager, IMapper mapper, ILogger<TagController> logger)
        {
            ArgumentNullException.ThrowIfNull(tagManager);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(logger);

            _tagManager = tagManager;
            _mapper = mapper;
            _logger = logger;
        }

        #endregion constructor

        [HttpGet(Name = "GetTags")]
        [TypeFilter(typeof(TagsResultFilter))]
        [TypeFilter(typeof(CollectionResultFilter))]
        public async Task<IActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var tagsResult = await _tagManager.GetEntitiesAsync(null, resourceCollectionParameters);
            if (!tagsResult.Success)
            {
                _logger.LogError("Failed to retrieve tags. Error: {ErrorMessage}. TraceId: {TraceId}", tagsResult.ErrorMessage, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An error occurred while retrieving tags.", HttpContext));
            }

            // Return 200 OK with empty array when no results (REST best practice)
            return Ok(tagsResult.Value ?? new PageList<Tag>());
        }

        [HttpGet("{id:int}", Name = "GetTagById")]
        [TypeFilter(typeof(TagResultFilter))]
        public async Task<IActionResult> Get(int id)
        {
            var tagResult = await _tagManager.GetTagByIdAsync(id);
            if (!tagResult.Success)
            {
                if (tagResult.IsNotFound)
                {
                    return NotFound(ProblemDetailsHelper.NotFound($"The tag : {id} not found.", HttpContext));
                }
                _logger.LogError("Failed to retrieve tag with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, tagResult.ErrorMessage, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An error occurred while retrieving the tag.", HttpContext));
            }

            return Ok(tagResult.Value);
        }

        [HttpGet("{name}", Name = "GetTagByName")]
        [TypeFilter(typeof(TagResultFilter))]
        public async Task<IActionResult> Get(string name)
        {
            var tagResult = await _tagManager.GetTagByNameAsync(name);
            if (!tagResult.Success)
            {
                if (tagResult.IsNotFound)
                {
                    return NotFound(ProblemDetailsHelper.NotFound($"The tag : {name} not found.", HttpContext));
                }
                _logger.LogError("Failed to retrieve tag with name {Name}. Error: {ErrorMessage}. TraceId: {TraceId}", name, tagResult.ErrorMessage, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An error occurred while retrieving the tag.", HttpContext));
            }

            return Ok(tagResult.Value);
        }

        // POST api/tags
        [HttpPost(Name = "AddTag")]
        [TypeFilter(typeof(TagResultFilter))]
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
                        return BadRequest(ProblemDetailsHelper.BadRequest(newTagResult.ErrorMessage, HttpContext));
                    }
                    _logger.LogError("Failed to create tag. Error: {ErrorMessage}. TraceId: {TraceId}", newTagResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while creating the tag.", HttpContext));
                }

                return CreatedAtRoute("GetTagById", new { id = newTagResult.Value.Id, version = "1.0" }, newTagResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating tag. TraceId: {TraceId}", HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while creating the tag.", HttpContext));
            }
        }

        // PUT api/tags/5
        [HttpPut("{id:int}", Name = "UpdateTag")]
        public async Task<IActionResult> Put(int id, [FromBody] ApiTagForUpdate tag)
        {
            if (tag == null || id <= 0)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest("Tag information is null or invalid id found", HttpContext));
            }

            try
            {
                var curTagResult = await _tagManager.GetTagByIdAsync(id);
                if (!curTagResult.Success)
                {
                    if (curTagResult.IsNotFound)
                    {
                        return NotFound(ProblemDetailsHelper.NotFound($"Tag with id {id} not found", HttpContext));
                    }
                    _logger.LogError("Failed to retrieve tag with id {Id} for updating. Error: {ErrorMessage}. TraceId: {TraceId}", id, curTagResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while retrieving the tag for update.", HttpContext));
                }

                var curTag = _mapper.Map(tag, curTagResult.Value);
                var updateResult = await _tagManager.UpdateAsync(curTag);

                if (!updateResult.Success)
                {
                    if (updateResult.IsNotFound)
                    {
                        return NotFound(ProblemDetailsHelper.NotFound($"Tag with id {id} not found", HttpContext));
                    }
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(ProblemDetailsHelper.BadRequest(updateResult.ErrorMessage, HttpContext));
                    }
                    _logger.LogError("Failed to update tag with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while updating the tag.", HttpContext));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while updating tag with id {Id}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while updating the tag.", HttpContext));
            }
        }

        // PATCH api/tags/5
        [HttpPatch("{id:int}", Name = "PatchTag")]
        public async Task<IActionResult> Patch(int id, [FromBody] JsonPatchDocument<ApiTagForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest("Patch information is null or invalid id found", HttpContext));
            }

            try
            {
                var curTagResult = await _tagManager.GetTagByIdAsync(id);
                if (!curTagResult.Success)
                {
                    if (curTagResult.IsNotFound)
                    {
                        return NotFound(ProblemDetailsHelper.NotFound($"Tag with id {id} not found", HttpContext));
                    }
                    _logger.LogError("Failed to retrieve tag with id {Id} for patching. Error: {ErrorMessage}. TraceId: {TraceId}", id, curTagResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while retrieving the tag for update.", HttpContext));
                }

                var tag2Update = _mapper.Map<ApiTagForUpdate>(curTagResult.Value);
                patchDoc.ApplyTo(tag2Update, ModelState);
                if (!TryValidateModel(tag2Update))
                {
                    return BadRequest(ProblemDetailsHelper.ValidationError(ModelState, HttpContext));
                }
                _mapper.Map(tag2Update, curTagResult.Value);

                var updateResult = await _tagManager.UpdateAsync(curTagResult.Value);
                if (!updateResult.Success)
                {
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(ProblemDetailsHelper.BadRequest(updateResult.ErrorMessage, HttpContext));
                    }
                    _logger.LogError("Failed to patch tag with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while patching the tag.", HttpContext));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while patching tag with id {Id}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while patching the tag.", HttpContext));
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
                        return NotFound(ProblemDetailsHelper.NotFound($"Tag with id {id} not found", HttpContext));
                    }
                    _logger.LogError("Failed to deactivate tag with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, deleteResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while deactivating the tag.", HttpContext));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while deactivating tag with id {Id}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while deactivating the tag.", HttpContext));
            }
        }
    }
}
