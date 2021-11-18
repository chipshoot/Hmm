using AutoMapper;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Controllers
{
    [Route("api/noterenders")]
    public class NoteRenderController : Controller
    {
        #region private fields

        private readonly INoteRenderManager _renderManager;
        private readonly IMapper _mapper;

        #endregion private fields

        #region constructor

        public NoteRenderController(INoteRenderManager renderManager, IMapper mapper)
        {
            Guard.Against<ArgumentNullException>(renderManager == null, nameof(renderManager));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));

            _renderManager = renderManager;
            _mapper = mapper;
        }

        #endregion constructor

        [HttpGet(Name = "GetNoteRenders")]
        [NoteRendersResultFilter]
        public async Task<IActionResult> Get()
        {
            var renders = await _renderManager.GetEntitiesAsync();
            var renderLst = renders.ToList();
            if (!renderLst.Any())
            {
                return NotFound();
            }

            return Ok(renderLst);
        }

        [HttpGet("{id:int}", Name = "GetNoteRenderById")]
        [NoteRendersResultFilter]
        public async Task<IActionResult> Get(int id)
        {
            var render = await _renderManager.GetEntityByIdAsync(id);
            if (render == null)
            {
                return NotFound($"The note render: {id} not found");
            }

            return Ok(render);
        }

        // POST api/renders
        [HttpPost(Name = "AddNoteRender")]
        [NoteRenderResultFilter]
        public async Task<IActionResult> Post([FromBody] ApiNoteRenderForCreate render)
        {
            try
            {
                var noteRender = _mapper.Map<ApiNoteRenderForCreate, NoteRender>(render);
                var newRender = await _renderManager.CreateAsync(noteRender);

                if (newRender == null)
                {
                    return BadRequest($"Internal error found when try to insert note render: {render.Name}");
                }

                return Created("", newRender);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT api/renders/5
        [HttpPut("{id:int}", Name = "UpdateNoteRender")]
        public async Task<IActionResult> Put(int id, [FromBody] ApiNoteRenderForUpdate render)
        {
            if (render == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("note render information is null or invalid id found"));
            }

            try
            {
                var curRender = await _renderManager.GetEntityByIdAsync(id);
                if (curRender == null)
                {
                    return BadRequest($"The note render {id} cannot be found.");
                }

                curRender = _mapper.Map(render, curRender);
                var apiNewRender = await _renderManager.UpdateAsync(curRender);
                if (apiNewRender == null)
                {
                    return BadRequest(_renderManager.ProcessResult.MessageList);
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PATCH api/renders/5
        [HttpPatch("{id:int}", Name = "PatchNoteRender")]
        public async Task<IActionResult> Patch(int id, [FromBody] JsonPatchDocument<ApiNoteRenderForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var curRender = await _renderManager.GetEntityByIdAsync(id);
                if (curRender == null)
                {
                    return NotFound();
                }

                var render2Update = _mapper.Map<ApiNoteRenderForUpdate>(curRender);
                patchDoc.ApplyTo(render2Update);
                _mapper.Map(render2Update, curRender);

                var newRender = await _renderManager.UpdateAsync(curRender);
                if (newRender == null)
                {
                    return BadRequest(_renderManager.ProcessResult.MessageList);
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // DELETE api/renders/5
        [HttpDelete("{id:int}")]
        public ActionResult Delete(int id)
        {
            return NoContent();
        }
    }
}