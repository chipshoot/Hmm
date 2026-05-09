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
    /// Manages append-only service records for a specific automobile.
    /// </summary>
    [Authorize]
    [ApiController]
    [EnableCors("AllowCors")]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/automobiles/{autoId:int}/services")]
    [Produces("application/json")]
    public class ServiceRecordController : Controller
    {
        private readonly IServiceRecordManager _serviceManager;
        private readonly IAutoEntityManager<AutomobileInfo> _autoManager;
        private readonly IMapper _mapper;
        private readonly ILogger<ServiceRecordController> _logger;

        public ServiceRecordController(
            IServiceRecordManager serviceManager,
            IAutoEntityManager<AutomobileInfo> autoManager,
            IMapper mapper,
            ILogger<ServiceRecordController> logger)
        {
            ArgumentNullException.ThrowIfNull(serviceManager);
            ArgumentNullException.ThrowIfNull(autoManager);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(logger);

            _serviceManager = serviceManager;
            _autoManager = autoManager;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet(Name = "GetServiceRecords")]
        [TypeFilter(typeof(ServiceRecordsResultFilter))]
        [TypeFilter(typeof(CollectionResultFilter))]
        [ProducesResponseType(typeof(ApiEntityCollection<ApiServiceRecord>), StatusCodes.Status200OK)]
        public async Task<ActionResult> Get(int autoId, [FromQuery] ResourceCollectionParameters resourceParameters)
        {
            var result = await _serviceManager.GetByAutomobileAsync(autoId, resourceParameters);
            if (!result.Success)
            {
                _logger.LogWarning("Failed to get service records for auto {AutoId}: {Error}", autoId, result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return Ok(result.Value ?? new PageList<ServiceRecord>(Array.Empty<ServiceRecord>(), 0, 1, 20));
        }

        [HttpGet("{id:int}", Name = "GetServiceRecordById")]
        [TypeFilter(typeof(ServiceRecordResultFilter))]
        [ProducesResponseType(typeof(ApiServiceRecord), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetById(int autoId, int id)
        {
            var result = await _serviceManager.GetEntityByIdAsync(id);
            if (!result.Success)
            {
                return result.IsNotFound ? NotFound() : BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            var record = result.Value;
            if (record.AutomobileId != autoId)
            {
                return NotFound();
            }

            return Ok(record);
        }

        [HttpPost(Name = "AddServiceRecord")]
        [TypeFilter(typeof(ServiceRecordResultFilter))]
        [ProducesResponseType(typeof(ApiServiceRecord), StatusCodes.Status200OK)]
        public async Task<ActionResult> Post(int autoId, [FromBody] ApiServiceRecordForCreate apiRecord)
        {
            if (apiRecord == null)
            {
                return BadRequest(new ApiBadRequestResponse("Null service record payload"));
            }

            var carResult = await _autoManager.GetEntityByIdAsync(autoId);
            if (!carResult.Success)
            {
                return BadRequest(new ApiBadRequestResponse($"Cannot find automobile with id {autoId}"));
            }

            var record = _mapper.Map<ServiceRecord>(apiRecord);
            record.AutomobileId = autoId;

            var saveResult = await _serviceManager.CreateAsync(record);
            if (!saveResult.Success)
            {
                return BadRequest(new ApiBadRequestResponse(saveResult.ErrorMessage));
            }

            return Ok(saveResult.Value);
        }

        [HttpPut("{id:int}", Name = "UpdateServiceRecord")]
        [ProducesResponseType(typeof(ApiServiceRecord), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Put(int autoId, int id, [FromBody] ApiServiceRecordForUpdate apiRecord)
        {
            if (apiRecord == null)
            {
                return BadRequest(new ApiBadRequestResponse("Null service record payload"));
            }

            var existing = await _serviceManager.GetEntityByIdAsync(id);
            if (!existing.Success)
            {
                return existing.IsNotFound ? NotFound() : BadRequest(new ApiBadRequestResponse(existing.ErrorMessage));
            }

            if (existing.Value.AutomobileId != autoId)
            {
                return NotFound();
            }

            var record = existing.Value;
            _mapper.Map(apiRecord, record);

            var updateResult = await _serviceManager.UpdateAsync(record);
            if (!updateResult.Success)
            {
                return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
            }

            var dto = _mapper.Map<ApiServiceRecord>(updateResult.Value);
            return Ok(dto);
        }

        [HttpPatch("{id:int}", Name = "PatchServiceRecord")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Patch(int autoId, int id, [FromBody] JsonPatchDocument<ApiServiceRecordForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch document is null or id invalid"));
            }

            var existing = await _serviceManager.GetEntityByIdAsync(id);
            if (!existing.Success)
            {
                return existing.IsNotFound ? NotFound() : BadRequest(new ApiBadRequestResponse(existing.ErrorMessage));
            }

            if (existing.Value.AutomobileId != autoId)
            {
                return NotFound();
            }

            var record = existing.Value;
            var dto = _mapper.Map<ApiServiceRecordForUpdate>(record);
            patchDoc.ApplyTo(dto);
            _mapper.Map(dto, record);

            var updateResult = await _serviceManager.UpdateAsync(record);
            if (!updateResult.Success)
            {
                return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
            }

            return NoContent();
        }

        [HttpDelete("{id:int}", Name = "DeleteServiceRecord")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(int autoId, int id)
        {
            var existing = await _serviceManager.GetEntityByIdAsync(id);
            if (!existing.Success)
            {
                return existing.IsNotFound ? NotFound() : BadRequest(new ApiBadRequestResponse(existing.ErrorMessage));
            }

            if (existing.Value.AutomobileId != autoId)
            {
                return NotFound();
            }

            var record = existing.Value;
            record.IsDeleted = true;

            var updateResult = await _serviceManager.UpdateAsync(record);
            return updateResult.Success ? NoContent() : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
