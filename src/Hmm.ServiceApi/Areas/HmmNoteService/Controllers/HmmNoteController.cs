using AutoMapper;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Filters;
using Hmm.Utility.Dal.Query;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Controllers
{
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/notes")]
    public class HmmNoteController : Controller
    {
        #region private fields

        private readonly IHmmNoteManager _noteManager;
        private readonly IMapper _mapper;

        #endregion private fields

        #region constructor

        public HmmNoteController(IHmmNoteManager noteManager, IMapper mapper)
        {
            Guard.Against<ArgumentNullException>(noteManager == null, nameof(noteManager));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));

            _noteManager = noteManager;
            _mapper = mapper;
        }

        #endregion constructor

        [HttpGet(Name = "GetNotes")]
        [NotesResultFilter]
        [PaginationFilter]
        public async Task<IActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var noteList = await _noteManager.GetNotesAsync(null, false, resourceCollectionParameters);
            if (!noteList.Any())
            {
                return NotFound();
            }

            return Ok(noteList);
        }

        [HttpGet("{id:int}", Name = "GetNoteById")]
        [NoteResultFilter]
        public async Task<IActionResult> Get(int id)
        {
            var note = await _noteManager.GetNoteByIdAsync(id);
            if (note == null)
            {
                return NotFound($"The note: {id} not found");
            }

            return Ok(note);
        }

        // POST api/notes
        [HttpPost(Name = "AddNote")]
        [NoteResultFilter]
        public async Task<IActionResult> Post([FromBody] ApiNoteForCreate note)
        {
            try
            {
                var noteNote = _mapper.Map<ApiNoteForCreate, HmmNote>(note);
                var newNote = await _noteManager.CreateAsync(noteNote);

                if (newNote == null)
                {
                    return BadRequest($"Internal error found when try to insert note: {note.Subject}");
                }

                return Created("", newNote);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT api/notes/5
        [HttpPut("{id:int}", Name = "UpdateNote")]
        public async Task<IActionResult> Put(int id, [FromBody] ApiNoteForUpdate note)
        {
            if (note == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("note information is null or invalid id found"));
            }

            try
            {
                var curNote = await _noteManager.GetNoteByIdAsync(id);
                if (curNote == null)
                {
                    return BadRequest($"The note {id} cannot be found.");
                }

                curNote = _mapper.Map(note, curNote);
                var newNote = await _noteManager.UpdateAsync(curNote);
                if (newNote == null)
                {
                    return BadRequest(_noteManager.ProcessResult.MessageList);
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
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
                var curNote = await _noteManager.GetNoteByIdAsync(id);
                if (curNote == null)
                {
                    return NotFound();
                }

                var note2Update = _mapper.Map<ApiNoteForUpdate>(curNote);
                patchDoc.ApplyTo(note2Update);
                _mapper.Map(note2Update, curNote);

                var newNote = await _noteManager.UpdateAsync(curNote);
                if (newNote == null)
                {
                    return BadRequest(_noteManager.ProcessResult.MessageList);
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // DELETE api/notes/5
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("invalid id found"));
            }

            try
            {
                var curNote= await _noteManager.GetNoteByIdAsync(id);
                if (curNote == null)
                {
                    return BadRequest($"The note {id} cannot be found.");
                }

                curNote.IsDeleted = true;
                var apiUpdatedNote = await _noteManager.UpdateAsync(curNote);
                if (apiUpdatedNote == null)
                {
                    return BadRequest(_noteManager.ProcessResult.MessageList);
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}