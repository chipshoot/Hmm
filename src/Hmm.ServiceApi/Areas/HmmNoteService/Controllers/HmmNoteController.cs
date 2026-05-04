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
    /// <summary>
    /// Manages note CRUD operations.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/notes")]
    [Produces("application/json")]
    public class HmmNoteController : Controller
    {
        private readonly IHmmNoteManager _noteManager;
        private readonly INoteTagAssociationManager _noteTagAssociationManager;
        private readonly IMapper _mapper;
        private readonly ILogger<HmmNoteController> _logger;

        public HmmNoteController(IHmmNoteManager noteManager, INoteTagAssociationManager noteTagAssociationManager, IMapper mapper, ILogger<HmmNoteController> logger)
        {
            ArgumentNullException.ThrowIfNull(noteManager);
            ArgumentNullException.ThrowIfNull(noteTagAssociationManager);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(logger);

            _noteManager = noteManager;
            _noteTagAssociationManager = noteTagAssociationManager;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a paginated list of notes.
        /// </summary>
        /// <param name="resourceCollectionParameters">Pagination and sorting parameters.</param>
        /// <returns>A paginated list of notes.</returns>
        [HttpGet(Name = "GetNotes")]
        [TypeFilter(typeof(NotesResultFilter))]
        [TypeFilter(typeof(CollectionResultFilter))]
        [ProducesResponseType(typeof(ApiEntityCollection<ApiNote>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var noteListResult = await _noteManager.GetNotesAsync(null, false, resourceCollectionParameters);
            if (!noteListResult.Success)
            {
                _logger.LogError("Failed to retrieve notes. Error: {ErrorMessage}, TraceId: {TraceId}",
                    noteListResult.ErrorMessage, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An error occurred while retrieving notes.", HttpContext));
            }

            // Return 200 OK with empty array when no results (REST best practice)
            return Ok(noteListResult.Value ?? new PageList<HmmNote>());
        }

        /// <summary>
        /// Retrieves a single note by its identifier.
        /// </summary>
        /// <param name="id">The note identifier.</param>
        /// <returns>The note matching the specified identifier.</returns>
        [HttpGet("{id:int}", Name = "GetNoteById")]
        [TypeFilter(typeof(NoteResultFilter))]
        [ProducesResponseType(typeof(ApiNote), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(int id)
        {
            var noteResult = await _noteManager.GetNoteByIdAsync(id);
            if (!noteResult.Success)
            {
                if (noteResult.IsNotFound)
                {
                    return NotFound(ProblemDetailsHelper.NotFound($"The note {id} not found.", HttpContext));
                }
                _logger.LogError("Failed to retrieve note {NoteId}. Error: {ErrorMessage}, TraceId: {TraceId}",
                    id, noteResult.ErrorMessage, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An error occurred while retrieving the note.", HttpContext));
            }

            return Ok(noteResult.Value);
        }

        /// <summary>
        /// Creates a new note.
        /// </summary>
        /// <param name="note">The note data for creation.</param>
        /// <returns>The newly created note.</returns>
        [HttpPost(Name = "AddNote")]
        [TypeFilter(typeof(NoteResultFilter))]
        [ProducesResponseType(typeof(ApiNote), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Post([FromBody] ApiNoteForCreate note)
        {
            try
            {
                var noteNote = _mapper.Map<ApiNoteForCreate, HmmNote>(note);
                var newNoteResult = await _noteManager.CreateAsync(noteNote);

                if (!newNoteResult.Success)
                {
                    if (newNoteResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(ProblemDetailsHelper.BadRequest(newNoteResult.ErrorMessage, HttpContext));
                    }
                    _logger.LogError("Failed to create note. Error: {ErrorMessage}, TraceId: {TraceId}",
                        newNoteResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while creating the note.", HttpContext));
                }

                return CreatedAtRoute("GetNoteById", new { id = newNoteResult.Value.Id, version = "1.0" }, newNoteResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while creating note. TraceId: {TraceId}", HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while creating the note.", HttpContext));
            }
        }

        /// <summary>
        /// Updates an existing note.
        /// </summary>
        /// <param name="id">The note identifier.</param>
        /// <param name="note">The updated note data.</param>
        /// <returns>No content on success.</returns>
        [HttpPut("{id:int}", Name = "UpdateNote")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Put(int id, [FromBody] ApiNoteForUpdate note)
        {
            if (note == null || id <= 0)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest("Note information is null or invalid id found", HttpContext));
            }

            try
            {
                var curNoteResult = await _noteManager.GetNoteByIdAsync(id);
                if (!curNoteResult.Success)
                {
                    if (curNoteResult.IsNotFound)
                    {
                        return NotFound(ProblemDetailsHelper.NotFound($"Note with id {id} not found", HttpContext));
                    }
                    _logger.LogError("Failed to retrieve note {NoteId} for update. Error: {ErrorMessage}, TraceId: {TraceId}",
                        id, curNoteResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while retrieving the note.", HttpContext));
                }

                var curNote = _mapper.Map(note, curNoteResult.Value);
                var updateResult = await _noteManager.UpdateAsync(curNote);

                if (!updateResult.Success)
                {
                    if (updateResult.IsNotFound)
                    {
                        return NotFound(ProblemDetailsHelper.NotFound($"Note with id {id} not found", HttpContext));
                    }
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(ProblemDetailsHelper.BadRequest(updateResult.ErrorMessage, HttpContext));
                    }
                    _logger.LogError("Failed to update note {NoteId}. Error: {ErrorMessage}, TraceId: {TraceId}",
                        id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while updating the note.", HttpContext));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while updating note {NoteId}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while updating the note.", HttpContext));
            }
        }

        /// <summary>
        /// Applies a tag to a note.
        /// </summary>
        /// <param name="id">The note identifier.</param>
        /// <param name="tag">The tag to apply.</param>
        /// <returns>No content on success.</returns>
        [HttpPut("{id:int}/applyTag", Name = "ApplyTagToNote")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApplyTag(int id, [FromBody] ApiTagForApply tag)
        {
            if (tag == null || string.IsNullOrWhiteSpace(tag.Name))
            {
                return BadRequest(ProblemDetailsHelper.BadRequest("Tag information is null", HttpContext));
            }

            try
            {
                var tagToApply = _mapper.Map<Tag>(tag);
                var tagListResult = await _noteTagAssociationManager.ApplyTagToNoteAsync(id, tagToApply);

                if (!tagListResult.Success)
                {
                    if (tagListResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(ProblemDetailsHelper.BadRequest(tagListResult.ErrorMessage, HttpContext));
                    }
                    _logger.LogError("Failed to apply tag to note {NoteId}. Error: {ErrorMessage}, TraceId: {TraceId}",
                        id, tagListResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while applying the tag.", HttpContext));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while applying tag to note {NoteId}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while applying the tag.", HttpContext));
            }
        }

        /// <summary>
        /// Partially updates a note using a JSON Patch document.
        /// </summary>
        /// <param name="id">The note identifier.</param>
        /// <param name="patchDoc">The JSON Patch document.</param>
        /// <returns>No content on success.</returns>
        [HttpPatch("{id:int}", Name = "PatchNote")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Patch(int id, [FromBody] JsonPatchDocument<ApiNoteForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest("Patch information is null or invalid id found", HttpContext));
            }

            try
            {
                var curNoteResult = await _noteManager.GetNoteByIdAsync(id);
                if (!curNoteResult.Success)
                {
                    if (curNoteResult.IsNotFound)
                    {
                        return NotFound(ProblemDetailsHelper.NotFound($"Note with id {id} not found", HttpContext));
                    }
                    _logger.LogError("Failed to retrieve note {NoteId} for patching. Error: {ErrorMessage}, TraceId: {TraceId}",
                        id, curNoteResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while retrieving the note.", HttpContext));
                }

                var note2Update = _mapper.Map<ApiNoteForUpdate>(curNoteResult.Value);
                patchDoc.ApplyTo(note2Update, ModelState);
                if (!TryValidateModel(note2Update))
                {
                    return BadRequest(ProblemDetailsHelper.ValidationError(ModelState, HttpContext));
                }
                _mapper.Map(note2Update, curNoteResult.Value);

                var updateResult = await _noteManager.UpdateAsync(curNoteResult.Value);
                if (!updateResult.Success)
                {
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(ProblemDetailsHelper.BadRequest(updateResult.ErrorMessage, HttpContext));
                    }
                    _logger.LogError("Failed to patch note {NoteId}. Error: {ErrorMessage}, TraceId: {TraceId}",
                        id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while updating the note.", HttpContext));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while patching note {NoteId}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while updating the note.", HttpContext));
            }
        }

        /// <summary>
        /// Soft-deletes a note by setting its IsDeleted flag.
        /// </summary>
        /// <param name="id">The note identifier.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var curNoteResult = await _noteManager.GetNoteByIdAsync(id);
                if (!curNoteResult.Success)
                {
                    if (curNoteResult.IsNotFound)
                    {
                        return NotFound(ProblemDetailsHelper.NotFound($"Note with id {id} not found", HttpContext));
                    }
                    _logger.LogError("Failed to retrieve note {NoteId} for deletion. Error: {ErrorMessage}, TraceId: {TraceId}",
                        id, curNoteResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while retrieving the note.", HttpContext));
                }

                curNoteResult.Value.IsDeleted = true;
                var updateResult = await _noteManager.UpdateAsync(curNoteResult.Value);

                if (!updateResult.Success)
                {
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(ProblemDetailsHelper.BadRequest(updateResult.ErrorMessage, HttpContext));
                    }
                    _logger.LogError("Failed to delete note {NoteId}. Error: {ErrorMessage}, TraceId: {TraceId}",
                        id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while deleting the note.", HttpContext));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while deleting note {NoteId}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while deleting the note.", HttpContext));
            }
        }
    }
}
