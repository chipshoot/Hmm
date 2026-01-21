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
    [Route("/v{version:apiVersion}/notes")]
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

        [HttpGet(Name = "GetNotes")]
        [NotesResultFilter]
        [CollectionResultFilter]
        public async Task<IActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var noteListResult = await _noteManager.GetNotesAsync(null, false, resourceCollectionParameters);
            if (!noteListResult.Success)
            {
                _logger.LogError("Failed to retrieve notes. Error: {ErrorMessage}, TraceId: {TraceId}",
                    noteListResult.ErrorMessage, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving notes.");
            }

            if (noteListResult.Value == null || !noteListResult.Value.Any())
            {
                return NotFound();
            }

            return Ok(noteListResult.Value);
        }

        [HttpGet("{id:int}", Name = "GetNoteById")]
        [NoteResultFilter]
        public async Task<IActionResult> Get(int id)
        {
            var noteResult = await _noteManager.GetNoteByIdAsync(id);
            if (!noteResult.Success)
            {
                if (noteResult.IsNotFound)
                {
                    return NotFound($"The note {id} not found.");
                }
                _logger.LogError("Failed to retrieve note {NoteId}. Error: {ErrorMessage}, TraceId: {TraceId}",
                    id, noteResult.ErrorMessage, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the note.");
            }

            return Ok(noteResult.Value);
        }

        // POST api/notes
        [HttpPost(Name = "AddNote")]
        [NoteResultFilter]
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
                        return BadRequest(new ApiBadRequestResponse(newNoteResult.ErrorMessage));
                    }
                    _logger.LogError("Failed to create note. Error: {ErrorMessage}, TraceId: {TraceId}",
                        newNoteResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the note.");
                }

                return Created("", newNoteResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while creating note. TraceId: {TraceId}", HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while creating the note.");
            }
        }

        // PUT api/notes/5
        [HttpPut("{id:int}", Name = "UpdateNote")]
        public async Task<IActionResult> Put(int id, [FromBody] ApiNoteForUpdate note)
        {
            if (note == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Note information is null or invalid id found"));
            }

            try
            {
                var curNoteResult = await _noteManager.GetNoteByIdAsync(id);
                if (!curNoteResult.Success)
                {
                    if (curNoteResult.IsNotFound)
                    {
                        return NotFound($"Note with id {id} not found");
                    }
                    _logger.LogError("Failed to retrieve note {NoteId} for update. Error: {ErrorMessage}, TraceId: {TraceId}",
                        id, curNoteResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the note.");
                }

                var curNote = _mapper.Map(note, curNoteResult.Value);
                var updateResult = await _noteManager.UpdateAsync(curNote);

                if (!updateResult.Success)
                {
                    if (updateResult.IsNotFound)
                    {
                        return NotFound($"Note with id {id} not found");
                    }
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
                    }
                    _logger.LogError("Failed to update note {NoteId}. Error: {ErrorMessage}, TraceId: {TraceId}",
                        id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the note.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while updating note {NoteId}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while updating the note.");
            }
        }

        // PUT api/notes/5/applyTag
        [HttpPut("{id:int}/applyTag", Name = "ApplyTagToNote")]
        public async Task<IActionResult> ApplyTag(int id, [FromBody] ApiTagForApply tag)
        {
            if (tag == null || string.IsNullOrWhiteSpace(tag.Name))
            {
                return BadRequest(new ApiBadRequestResponse("Tag information is null"));
            }

            try
            {
                var tagToApply = _mapper.Map<Tag>(tag);
                var tagListResult = await _noteTagAssociationManager.ApplyTagToNoteAsync(id, tagToApply);

                if (!tagListResult.Success)
                {
                    if (tagListResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(new ApiBadRequestResponse(tagListResult.ErrorMessage));
                    }
                    _logger.LogError("Failed to apply tag to note {NoteId}. Error: {ErrorMessage}, TraceId: {TraceId}",
                        id, tagListResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while applying the tag.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while applying tag to note {NoteId}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while applying the tag.");
            }
        }

        // PATCH api/notes/5
        [HttpPatch("{id:int}", Name = "PatchNote")]
        public async Task<IActionResult> Patch(int id, [FromBody] JsonPatchDocument<ApiNoteForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var curNoteResult = await _noteManager.GetNoteByIdAsync(id);
                if (!curNoteResult.Success)
                {
                    if (curNoteResult.IsNotFound)
                    {
                        return NotFound($"Note with id {id} not found");
                    }
                    _logger.LogError("Failed to retrieve note {NoteId} for patching. Error: {ErrorMessage}, TraceId: {TraceId}",
                        id, curNoteResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the note.");
                }

                var note2Update = _mapper.Map<ApiNoteForUpdate>(curNoteResult.Value);
                patchDoc.ApplyTo(note2Update);
                _mapper.Map(note2Update, curNoteResult.Value);

                var updateResult = await _noteManager.UpdateAsync(curNoteResult.Value);
                if (!updateResult.Success)
                {
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
                    }
                    _logger.LogError("Failed to patch note {NoteId}. Error: {ErrorMessage}, TraceId: {TraceId}",
                        id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the note.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while patching note {NoteId}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while updating the note.");
            }
        }

        // DELETE api/notes/5
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var curNoteResult = await _noteManager.GetNoteByIdAsync(id);
                if (!curNoteResult.Success)
                {
                    if (curNoteResult.IsNotFound)
                    {
                        return NotFound($"Note with id {id} not found");
                    }
                    _logger.LogError("Failed to retrieve note {NoteId} for deletion. Error: {ErrorMessage}, TraceId: {TraceId}",
                        id, curNoteResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the note.");
                }

                curNoteResult.Value.IsDeleted = true;
                var updateResult = await _noteManager.UpdateAsync(curNoteResult.Value);

                if (!updateResult.Success)
                {
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
                    }
                    _logger.LogError("Failed to delete note {NoteId}. Error: {ErrorMessage}, TraceId: {TraceId}",
                        id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the note.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while deleting note {NoteId}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while deleting the note.");
            }
        }
    }
}