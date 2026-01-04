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
    [Route("/v{version:apiVersion}/notes")]
    public class HmmNoteController : Controller
    {
        private readonly IHmmNoteManager _noteManager;
        private readonly IMapper _mapper;

        public HmmNoteController(IHmmNoteManager noteManager, IMapper mapper)
        {
            ArgumentNullException.ThrowIfNull(noteManager);
            ArgumentNullException.ThrowIfNull(mapper);

            _noteManager = noteManager;
            _mapper = mapper;
        }

        [HttpGet(Name = "GetNotes")]
        [NotesResultFilter]
        [CollectionResultFilter]
        public async Task<IActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var noteListResult = await _noteManager.GetNotesAsync(null, false, resourceCollectionParameters);
            if (!noteListResult.Success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, noteListResult.ErrorMessage);
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
                return StatusCode(StatusCodes.Status500InternalServerError, noteResult.ErrorMessage);
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
                    return StatusCode(StatusCodes.Status500InternalServerError, newNoteResult.ErrorMessage);
                }

                return Created("", newNoteResult.Value);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
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
                    return StatusCode(StatusCodes.Status500InternalServerError, curNoteResult.ErrorMessage);
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
                    return StatusCode(StatusCodes.Status500InternalServerError, updateResult.ErrorMessage);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
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
                var curNoteResult = await _noteManager.GetNoteByIdAsync(id);
                if (!curNoteResult.Success)
                {
                    if (curNoteResult.IsNotFound)
                    {
                        return NotFound($"The note {id} cannot be found.");
                    }
                    return StatusCode(StatusCodes.Status500InternalServerError, curNoteResult.ErrorMessage);
                }

                var tagToApply = _mapper.Map<Tag>(tag);
                var tagListResult = await _noteManager.ApplyTag(curNoteResult.Value, tagToApply);

                if (!tagListResult.Success)
                {
                    if (tagListResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(new ApiBadRequestResponse(tagListResult.ErrorMessage));
                    }
                    return StatusCode(StatusCodes.Status500InternalServerError, tagListResult.ErrorMessage);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
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
                    return StatusCode(StatusCodes.Status500InternalServerError, curNoteResult.ErrorMessage);
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
                    return StatusCode(StatusCodes.Status500InternalServerError, updateResult.ErrorMessage);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
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
                    return StatusCode(StatusCodes.Status500InternalServerError, curNoteResult.ErrorMessage);
                }

                curNoteResult.Value.IsDeleted = true;
                var updateResult = await _noteManager.UpdateAsync(curNoteResult.Value);

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
    }
}