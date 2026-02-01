using AutoMapper;
using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Filters;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Controllers
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/api/v{version:apiVersion}/automobiles/gasstations")]
    public class GasStationController : Controller
    {
        private readonly GasStationManager _stationManager;
        private readonly IMapper _mapper;
        private readonly ILogger<GasStationController> _logger;

        public GasStationController(
            GasStationManager stationManager,
            IMapper mapper,
            ILogger<GasStationController> logger)
        {
            ArgumentNullException.ThrowIfNull(stationManager);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(logger);

            _stationManager = stationManager;
            _mapper = mapper;
            _logger = logger;
        }

        // GET api/automobiles/gasstations
        [HttpGet(Name = "GetGasStations")]
        [TypeFilter(typeof(GasStationsResultFilter))]
        [TypeFilter(typeof(CollectionResultFilter))]
        public async Task<ActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var result = await _stationManager.GetEntitiesAsync(resourceCollectionParameters);
            if (!result.Success)
            {
                _logger.LogWarning("Failed to get gas stations: {Error}", result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            if (result.Value == null || result.Value.Count == 0)
            {
                return NotFound();
            }

            return Ok(result.Value);
        }

        // GET api/automobiles/gasstations/active
        [HttpGet("active", Name = "GetActiveGasStations")]
        [TypeFilter(typeof(GasStationsResultFilter))]
        [TypeFilter(typeof(CollectionResultFilter))]
        public async Task<ActionResult> GetActive([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var result = await _stationManager.GetActiveStationsAsync(resourceCollectionParameters);
            if (!result.Success)
            {
                _logger.LogWarning("Failed to get active gas stations: {Error}", result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            if (result.Value == null || result.Value.Count == 0)
            {
                return NotFound();
            }

            return Ok(result.Value);
        }

        // GET api/automobiles/gasstations/1
        [HttpGet("{id:int}", Name = "GetGasStationById")]
        [TypeFilter(typeof(GasStationResultFilter))]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _stationManager.GetEntityByIdAsync(id);
            if (!result.Success)
            {
                if (result.IsNotFound)
                {
                    return NotFound();
                }
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return Ok(result.Value);
        }

        // GET api/automobiles/gasstations/byname/{name}
        [HttpGet("byname/{name}", Name = "GetGasStationByName")]
        [TypeFilter(typeof(GasStationResultFilter))]
        public async Task<IActionResult> GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new ApiBadRequestResponse("Station name is required"));
            }

            var result = await _stationManager.GetByNameAsync(name);
            if (!result.Success)
            {
                if (result.IsNotFound)
                {
                    return NotFound();
                }
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return Ok(result.Value);
        }

        // POST api/automobiles/gasstations
        [HttpPost(Name = "AddGasStation")]
        [TypeFilter(typeof(GasStationResultFilter))]
        public async Task<ActionResult> Post(ApiGasStationForCreate apiStation)
        {
            if (apiStation == null)
            {
                return BadRequest(new ApiBadRequestResponse("Station data is required"));
            }

            try
            {
                var station = _mapper.Map<GasStation>(apiStation);
                var result = await _stationManager.CreateAsync(station);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to create gas station: {Error}", result.ErrorMessage);
                    return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
                }

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating gas station");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT api/automobiles/gasstations/5
        [HttpPut("{id:int}", Name = "UpdateGasStation")]
        public async Task<IActionResult> Put(int id, ApiGasStationForUpdate apiStation)
        {
            if (apiStation == null)
            {
                return BadRequest(new ApiBadRequestResponse("Station data is required"));
            }

            var getResult = await _stationManager.GetEntityByIdAsync(id);
            if (!getResult.Success)
            {
                if (getResult.IsNotFound)
                {
                    return NotFound();
                }
                return BadRequest(new ApiBadRequestResponse(getResult.ErrorMessage));
            }

            var curStation = getResult.Value;
            _mapper.Map(apiStation, curStation);

            var updateResult = await _stationManager.UpdateAsync(curStation);
            if (!updateResult.Success)
            {
                return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
            }

            return NoContent();
        }

        // PATCH api/automobiles/gasstations/1
        [HttpPatch("{id:int}", Name = "PatchGasStation")]
        public async Task<ActionResult> Patch(int id, JsonPatchDocument<ApiGasStationForUpdate> patchDocument)
        {
            if (patchDocument == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var getResult = await _stationManager.GetEntityByIdAsync(id);
                if (!getResult.Success)
                {
                    if (getResult.IsNotFound)
                    {
                        return NotFound();
                    }
                    return BadRequest(new ApiBadRequestResponse(getResult.ErrorMessage));
                }

                var station = getResult.Value;
                var stationToPatch = _mapper.Map<ApiGasStationForUpdate>(station);
                patchDocument.ApplyTo(stationToPatch, ModelState);

                if (!TryValidateModel(stationToPatch))
                {
                    return ValidationProblem(ModelState);
                }

                _mapper.Map(stationToPatch, station);
                var updateResult = await _stationManager.UpdateAsync(station);

                if (!updateResult.Success)
                {
                    return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error patching gas station {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // DELETE api/automobiles/gasstations/1
        [HttpDelete("{id:int}", Name = "DeleteGasStation")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var getResult = await _stationManager.GetEntityByIdAsync(id);
                if (!getResult.Success)
                {
                    if (getResult.IsNotFound)
                    {
                        return NotFound();
                    }
                    return BadRequest(new ApiBadRequestResponse(getResult.ErrorMessage));
                }

                var station = getResult.Value;
                station.IsActive = false;

                var updateResult = await _stationManager.UpdateAsync(station);
                if (!updateResult.Success)
                {
                    _logger.LogError("Failed to deactivate station {Id}: {Error}", id, updateResult.ErrorMessage);
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gas station {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
