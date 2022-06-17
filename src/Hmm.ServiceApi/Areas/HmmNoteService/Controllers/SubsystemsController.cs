using AutoMapper;
using Hmm.Core;
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
    [Route("/v{version:apiVersion}/subsystems")]
    [ApiController]
    public class SubsystemsController : Controller
    {
        #region private fields

        private readonly ISubsystemManager _systemManager;
        private readonly IMapper _mapper;

        #endregion private fields

        public SubsystemsController(ISubsystemManager systemManager, IMapper mapper)
        {
            Guard.Against<ArgumentNullException>(systemManager == null, nameof(systemManager));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));

            _systemManager = systemManager;
            _mapper = mapper;
        }

        [HttpGet(Name = "GetSubsystems")]
        [SubsystemsResultFilter]
        [PaginationFilter]
        public async Task<IActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var systemList = await _systemManager.GetEntitiesAsync(null, resourceCollectionParameters);
            if (!systemList.Any())
            {
                return NotFound();
            }

            return Ok(systemList);
        }

        [HttpGet("{id:int}", Name = "GetSubsystemById")]
        [SubsystemResultFilter]
        public async Task<IActionResult> Get(int id)
        {
            var subsystem = await _systemManager.GetEntityByIdAsync(id);
            if (subsystem == null)
            {
                return NotFound($"Subsystem: {id} not found.");
            }

            return Ok(subsystem);
        }

        // PUT api/subsystems/2
        [HttpPut("{id:int}", Name = "UpdateSubsystem")]
        public async Task<IActionResult> Put(int id, [FromBody] ApiSubsystemForUpdate system)
        {
            if (system == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("sub system information is null or invalid id found"));
            }

            try
            {
                var curSystem = await _systemManager.GetEntityByIdAsync(id);
                if (curSystem == null)
                {
                    return BadRequest($"The sub system : {id} cannot be found");
                }

                curSystem = _mapper.Map(system, curSystem);
                var apiNewSystem = await _systemManager.UpdateAsync(curSystem);
                if (apiNewSystem == null)
                {
                    return BadRequest(_systemManager.ProcessResult.MessageList);
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PATCH api/subsystems/2
        [HttpPatch("{id:int}", Name = "PatchSubsystem")]
        public async Task<IActionResult> Patch(int id, [FromBody] JsonPatchDocument<ApiSubsystemForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var curSystem = await _systemManager.GetEntityByIdAsync(id);
                if (curSystem == null)
                {
                    return NotFound();
                }

                var system2Update = _mapper.Map<ApiSubsystemForUpdate>(curSystem);
                patchDoc.ApplyTo(system2Update);
                _mapper.Map(system2Update, curSystem);

                var newSystem = await _systemManager.UpdateAsync(curSystem);
                if (newSystem == null)
                {
                    return BadRequest(_systemManager.ProcessResult.MessageList);
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