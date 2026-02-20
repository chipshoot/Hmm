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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Controllers
{
    /// <summary>
    /// Manages gas station CRUD operations.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/api/v{version:apiVersion}/automobiles/gasstations")]
    [Produces("application/json")]
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

        /// <summary>
        /// Retrieves a paginated list of all gas stations.
        /// </summary>
        /// <param name="resourceCollectionParameters">Pagination and sorting parameters.</param>
        /// <returns>A paginated list of gas stations.</returns>
        [HttpGet(Name = "GetGasStations")]
        [TypeFilter(typeof(GasStationsResultFilter))]
        [TypeFilter(typeof(CollectionResultFilter))]
        [ProducesResponseType(typeof(ApiEntityCollection<ApiGasStation>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var result = await _stationManager.GetEntitiesAsync(resourceCollectionParameters);
            if (!result.Success)
            {
                _logger.LogWarning("Failed to get gas stations: {Error}", result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return Ok(result.Value ?? new Hmm.Utility.Dal.Query.PageList<GasStation>(
                Array.Empty<GasStation>(), 0, 1, 10));
        }

        /// <summary>
        /// Retrieves a paginated list of active gas stations only.
        /// </summary>
        /// <param name="resourceCollectionParameters">Pagination and sorting parameters.</param>
        /// <returns>A paginated list of active gas stations.</returns>
        [HttpGet("active", Name = "GetActiveGasStations")]
        [TypeFilter(typeof(GasStationsResultFilter))]
        [TypeFilter(typeof(CollectionResultFilter))]
        [ProducesResponseType(typeof(ApiEntityCollection<ApiGasStation>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetActive([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var result = await _stationManager.GetActiveStationsAsync(resourceCollectionParameters);
            if (!result.Success)
            {
                _logger.LogWarning("Failed to get active gas stations: {Error}", result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return Ok(result.Value ?? new Hmm.Utility.Dal.Query.PageList<GasStation>(
                Array.Empty<GasStation>(), 0, 1, 10));
        }

        /// <summary>
        /// Retrieves a single gas station by identifier.
        /// </summary>
        /// <param name="id">The gas station identifier.</param>
        /// <returns>The gas station matching the specified identifier.</returns>
        [HttpGet("{id:int}", Name = "GetGasStationById")]
        [TypeFilter(typeof(GasStationResultFilter))]
        [ProducesResponseType(typeof(ApiGasStation), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Retrieves a gas station by its name.
        /// </summary>
        /// <param name="name">The gas station name.</param>
        /// <returns>The gas station matching the specified name.</returns>
        [HttpGet("byname/{name}", Name = "GetGasStationByName")]
        [TypeFilter(typeof(GasStationResultFilter))]
        [ProducesResponseType(typeof(ApiGasStation), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Creates a new gas station.
        /// </summary>
        /// <param name="apiStation">The gas station data for creation.</param>
        /// <returns>The newly created gas station.</returns>
        [HttpPost(Name = "AddGasStation")]
        [TypeFilter(typeof(GasStationResultFilter))]
        [ProducesResponseType(typeof(ApiGasStation), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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

        /// <summary>
        /// Updates an existing gas station.
        /// </summary>
        /// <param name="id">The gas station identifier.</param>
        /// <param name="apiStation">The updated gas station data.</param>
        /// <returns>No content on success.</returns>
        [HttpPut("{id:int}", Name = "UpdateGasStation")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Partially updates a gas station using a JSON Patch document.
        /// </summary>
        /// <param name="id">The gas station identifier.</param>
        /// <param name="patchDocument">The JSON Patch document.</param>
        /// <returns>No content on success.</returns>
        [HttpPatch("{id:int}", Name = "PatchGasStation")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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

        /// <summary>
        /// Deletes a gas station.
        /// </summary>
        /// <param name="id">The gas station identifier.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{id:int}", Name = "DeleteGasStation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
