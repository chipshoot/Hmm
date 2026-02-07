using AutoMapper;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity;
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
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/api/v{version:apiVersion}/authors")]
    [Produces("application/json")]
    public class AuthorController : Controller
    {
        #region private fields

        private readonly IAuthorManager _authorManager;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthorController> _logger;

        #endregion private fields

        #region constructor

        public AuthorController(IAuthorManager authorManager, IMapper mapper, ILogger<AuthorController> logger)
        {
            ArgumentNullException.ThrowIfNull(authorManager);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(logger);

            _authorManager = authorManager;
            _mapper = mapper;
            _logger = logger;
        }

        #endregion constructor

        [HttpGet(Name = "GetAuthors")]
        [TypeFilter(typeof(AuthorsResultFilter))]
        [TypeFilter(typeof(CollectionResultFilter))]
        [ProducesResponseType(typeof(ApiEntityCollection<ApiAuthor>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var authorsResult = await _authorManager.GetEntitiesAsync(null, resourceCollectionParameters);
            if (!authorsResult.Success)
            {
                _logger.LogError("Failed to retrieve authors. Error: {ErrorMessage}. TraceId: {TraceId}", authorsResult.ErrorMessage, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An error occurred while retrieving authors.", HttpContext));
            }

            // Return 200 OK with empty array when no results (REST best practice)
            return Ok(authorsResult.Value ?? new PageList<Author>());
        }

        [HttpGet("{id:int}", Name = "GetAuthorById")]
        [TypeFilter(typeof(AuthorResultFilter))]
        [ProducesResponseType(typeof(ApiAuthor), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(int id)
        {
            if (id <= 0)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest("Invalid author id", HttpContext));
            }

            var authorResult = await _authorManager.GetAuthorByIdAsync(id);
            if (!authorResult.Success)
            {
                if (authorResult.IsNotFound)
                {
                    return NotFound(ProblemDetailsHelper.NotFound($"The author {id} cannot be found.", HttpContext));
                }
                _logger.LogError("Failed to retrieve author with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, authorResult.ErrorMessage, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An error occurred while retrieving the author.", HttpContext));
            }

            return Ok(authorResult.Value);
        }

        // POST api/authors
        [HttpPost(Name = "AddAuthor")]
        [TypeFilter(typeof(AuthorResultFilter))]
        [ProducesResponseType(typeof(ApiAuthor), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Post(ApiAuthorForCreate author)
        {
            try
            {
                var usr = _mapper.Map<ApiAuthorForCreate, Author>(author);
                usr.IsActivated = true;
                var newAuthorResult = await _authorManager.CreateAsync(usr);

                if (!newAuthorResult.Success)
                {
                    if (newAuthorResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(ProblemDetailsHelper.BadRequest(newAuthorResult.ErrorMessage, HttpContext));
                    }
                    _logger.LogError("Failed to create author. Error: {ErrorMessage}. TraceId: {TraceId}", newAuthorResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while creating the author.", HttpContext));
                }

                return CreatedAtRoute("GetAuthorById", new { id = newAuthorResult.Value.Id, version = "1.0" }, newAuthorResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating author. TraceId: {TraceId}", HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while creating the author.", HttpContext));
            }
        }

        // PUT api/authors/5
        [HttpPut("{id:int}", Name = "UpdateAuthor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Put(int id, ApiAuthorForUpdate author)
        {
            if (author == null || id <= 0)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest("Author information is null or invalid id found", HttpContext));
            }

            try
            {
                var currentAuthor = _mapper.Map<Author>(author);
                currentAuthor.Id = id;
                var updateResult = await _authorManager.UpdateAsync(currentAuthor);

                if (!updateResult.Success)
                {
                    if (updateResult.IsNotFound)
                    {
                        return NotFound(ProblemDetailsHelper.NotFound($"Author with id {id} not found", HttpContext));
                    }
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(ProblemDetailsHelper.BadRequest(updateResult.ErrorMessage, HttpContext));
                    }
                    _logger.LogError("Failed to update author with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while updating the author.", HttpContext));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while updating author with id {Id}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while updating the author.", HttpContext));
            }
        }

        // PATCH api/authors/5
        [HttpPatch("{id:int}", Name = "PatchAuthor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Patch(int id, JsonPatchDocument<ApiAuthorForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest("Patch information is null or invalid id found", HttpContext));
            }

            try
            {
                var curUsrResult = await _authorManager.GetAuthorByIdAsync(id);
                if (!curUsrResult.Success)
                {
                    if (curUsrResult.IsNotFound)
                    {
                        return NotFound(ProblemDetailsHelper.NotFound($"Author with id {id} not found", HttpContext));
                    }
                    _logger.LogError("Failed to retrieve author with id {Id} for patching. Error: {ErrorMessage}. TraceId: {TraceId}", id, curUsrResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while retrieving the author for update.", HttpContext));
                }

                var author2Update = _mapper.Map<ApiAuthorForUpdate>(curUsrResult.Value);
                patchDoc.ApplyTo(author2Update, ModelState);
                if (!TryValidateModel(author2Update))
                {
                    return BadRequest(ProblemDetailsHelper.ValidationError(ModelState, HttpContext));
                }
                _mapper.Map(author2Update, curUsrResult.Value);

                var updateResult = await _authorManager.UpdateAsync(curUsrResult.Value);
                if (!updateResult.Success)
                {
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(ProblemDetailsHelper.BadRequest(updateResult.ErrorMessage, HttpContext));
                    }
                    _logger.LogError("Failed to patch author with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while patching the author.", HttpContext));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while patching author with id {Id}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while patching the author.", HttpContext));
            }
        }

        // DELETE api/authors/5
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var deleteResult = await _authorManager.DeActivateAsync(id);

                if (!deleteResult.Success)
                {
                    if (deleteResult.IsNotFound)
                    {
                        return NotFound(ProblemDetailsHelper.NotFound($"Author with id {id} not found", HttpContext));
                    }
                    _logger.LogError("Failed to deactivate author with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, deleteResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while deactivating the author.", HttpContext));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while deactivating author with id {Id}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while deactivating the author.", HttpContext));
            }
        }
    }
}
