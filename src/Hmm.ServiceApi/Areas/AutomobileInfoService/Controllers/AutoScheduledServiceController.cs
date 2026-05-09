using AutoMapper;
using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Filters;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Controllers
{
    /// <summary>
    /// Manages recurring / upcoming maintenance schedules for a specific automobile.
    /// </summary>
    [Authorize]
    [ApiController]
    [EnableCors("AllowCors")]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/automobiles/{autoId:int}/scheduled-services")]
    [Produces("application/json")]
    public class AutoScheduledServiceController : Controller
    {
        private readonly IAutoScheduledServiceManager _scheduleManager;
        private readonly IAutoEntityManager<AutomobileInfo> _autoManager;
        private readonly IMapper _mapper;
        private readonly ILogger<AutoScheduledServiceController> _logger;

        public AutoScheduledServiceController(
            IAutoScheduledServiceManager scheduleManager,
            IAutoEntityManager<AutomobileInfo> autoManager,
            IMapper mapper,
            ILogger<AutoScheduledServiceController> logger)
        {
            ArgumentNullException.ThrowIfNull(scheduleManager);
            ArgumentNullException.ThrowIfNull(autoManager);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(logger);

            _scheduleManager = scheduleManager;
            _autoManager = autoManager;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet(Name = "GetAutoScheduledServices")]
        [TypeFilter(typeof(AutoScheduledServicesResultFilter))]
        [TypeFilter(typeof(CollectionResultFilter))]
        [ProducesResponseType(typeof(ApiEntityCollection<ApiAutoScheduledService>), StatusCodes.Status200OK)]
        public async Task<ActionResult> Get(int autoId, [FromQuery] ResourceCollectionParameters resourceParameters)
        {
            var result = await _scheduleManager.GetByAutomobileAsync(autoId, resourceParameters);
            if (!result.Success)
            {
                _logger.LogWarning("Failed to get scheduled services for auto {AutoId}: {Error}", autoId, result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return Ok(result.Value ?? new PageList<AutoScheduledService>(Array.Empty<AutoScheduledService>(), 0, 1, 20));
        }

        [HttpGet("soonest", Name = "GetSoonestAutoScheduledService")]
        [TypeFilter(typeof(AutoScheduledServiceResultFilter))]
        [ProducesResponseType(typeof(ApiAutoScheduledService), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetSoonest(int autoId)
        {
            var result = await _scheduleManager.GetSoonestDueForAutomobileAsync(autoId);
            if (!result.Success)
            {
                return result.IsNotFound ? NotFound() : BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }
            return Ok(result.Value);
        }

        [HttpGet("{id:int}", Name = "GetAutoScheduledServiceById")]
        [TypeFilter(typeof(AutoScheduledServiceResultFilter))]
        [ProducesResponseType(typeof(ApiAutoScheduledService), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetById(int autoId, int id)
        {
            var result = await _scheduleManager.GetEntityByIdAsync(id);
            if (!result.Success)
            {
                return result.IsNotFound ? NotFound() : BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            var schedule = result.Value;
            if (schedule.AutomobileId != autoId)
            {
                return NotFound();
            }

            return Ok(schedule);
        }

        [HttpPost(Name = "AddAutoScheduledService")]
        [TypeFilter(typeof(AutoScheduledServiceResultFilter))]
        [ProducesResponseType(typeof(ApiAutoScheduledService), StatusCodes.Status200OK)]
        public async Task<ActionResult> Post(int autoId, [FromBody] ApiAutoScheduledServiceForCreate apiSchedule)
        {
            if (apiSchedule == null)
            {
                return BadRequest(new ApiBadRequestResponse("Null scheduled service payload"));
            }

            var carResult = await _autoManager.GetEntityByIdAsync(autoId);
            if (!carResult.Success)
            {
                return BadRequest(new ApiBadRequestResponse($"Cannot find automobile with id {autoId}"));
            }

            var schedule = _mapper.Map<AutoScheduledService>(apiSchedule);
            schedule.AutomobileId = autoId;

            var saveResult = await _scheduleManager.CreateAsync(schedule);
            if (!saveResult.Success)
            {
                return BadRequest(new ApiBadRequestResponse(saveResult.ErrorMessage));
            }

            return Ok(saveResult.Value);
        }

        [HttpPut("{id:int}", Name = "UpdateAutoScheduledService")]
        [ProducesResponseType(typeof(ApiAutoScheduledService), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Put(int autoId, int id, [FromBody] ApiAutoScheduledServiceForUpdate apiSchedule)
        {
            if (apiSchedule == null)
            {
                return BadRequest(new ApiBadRequestResponse("Null scheduled service payload"));
            }

            var existing = await _scheduleManager.GetEntityByIdAsync(id);
            if (!existing.Success)
            {
                return existing.IsNotFound ? NotFound() : BadRequest(new ApiBadRequestResponse(existing.ErrorMessage));
            }

            if (existing.Value.AutomobileId != autoId)
            {
                return NotFound();
            }

            var schedule = existing.Value;
            _mapper.Map(apiSchedule, schedule);

            var updateResult = await _scheduleManager.UpdateAsync(schedule);
            if (!updateResult.Success)
            {
                return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
            }

            var dto = _mapper.Map<ApiAutoScheduledService>(updateResult.Value);
            return Ok(dto);
        }

        [HttpPatch("{id:int}", Name = "PatchAutoScheduledService")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Patch(int autoId, int id, [FromBody] JsonPatchDocument<ApiAutoScheduledServiceForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch document is null or id invalid"));
            }

            var existing = await _scheduleManager.GetEntityByIdAsync(id);
            if (!existing.Success)
            {
                return existing.IsNotFound ? NotFound() : BadRequest(new ApiBadRequestResponse(existing.ErrorMessage));
            }

            if (existing.Value.AutomobileId != autoId)
            {
                return NotFound();
            }

            var schedule = existing.Value;
            var dto = _mapper.Map<ApiAutoScheduledServiceForUpdate>(schedule);
            patchDoc.ApplyTo(dto);
            _mapper.Map(dto, schedule);

            var updateResult = await _scheduleManager.UpdateAsync(schedule);
            if (!updateResult.Success)
            {
                return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
            }

            return NoContent();
        }

        [HttpDelete("{id:int}", Name = "DeleteAutoScheduledService")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(int autoId, int id)
        {
            var existing = await _scheduleManager.GetEntityByIdAsync(id);
            if (!existing.Success)
            {
                return existing.IsNotFound ? NotFound() : BadRequest(new ApiBadRequestResponse(existing.ErrorMessage));
            }

            if (existing.Value.AutomobileId != autoId)
            {
                return NotFound();
            }

            var schedule = existing.Value;
            schedule.IsDeleted = true;

            var updateResult = await _scheduleManager.UpdateAsync(schedule);
            return updateResult.Success ? NoContent() : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
