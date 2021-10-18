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

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var render = _renderManager.GetEntities().FirstOrDefault(r => r.Id == id);
            var ret = _mapper.Map<NoteRender, ApiNoteRender>(render);
            return Ok(ret);
        }

        // POST api/renders
        [HttpPost]
        public IActionResult Post([FromBody] ApiNoteRenderForCreate render)
        {
            try
            {
                var noteRender = _mapper.Map<ApiNoteRenderForCreate, NoteRender>(render);
                var newRender = _renderManager.Create(noteRender);

                if (newRender == null)
                {
                    return BadRequest();
                }

                var apiNewRender = _mapper.Map<NoteRender, ApiNoteRender>(newRender);

                return Ok(apiNewRender);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT api/renders/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] ApiNoteRenderForUpdate render)
        {
            if (render == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("note render information is null or invalid id found"));
            }

            try
            {
                var curRender = _renderManager.GetEntities().FirstOrDefault(r => r.Id == id);
                if (curRender == null)
                {
                    return NotFound();
                }

                curRender = _mapper.Map(render, curRender);
                var apiNewRender = _renderManager.Update(curRender);
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
        [HttpPatch("{id}")]
        public IActionResult Patch(int id, [FromBody] JsonPatchDocument<ApiNoteRenderForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var curRender = _renderManager.GetEntities().FirstOrDefault(r => r.Id == id);
                if (curRender == null)
                {
                    return NotFound();
                }

                var render2Update = _mapper.Map<ApiNoteRenderForUpdate>(curRender);
                patchDoc.ApplyTo(render2Update);
                _mapper.Map(render2Update, curRender);

                var newRender = _renderManager.Update(curRender);
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
        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            return NoContent();
        }
    }
}