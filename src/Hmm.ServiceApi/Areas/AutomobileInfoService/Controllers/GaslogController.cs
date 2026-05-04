using AutoMapper;
using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Filters;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Infrastructure;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.DtoEntity.Services;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Currency;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Controllers
{
    /// <summary>
    /// Manages gas log entries for automobiles.
    /// </summary>
    [Authorize]
    [ApiController]
    [EnableCors("AllowCors")]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/automobiles/{autoId:int}/gaslogs")]
    [Produces("application/json")]
    public class GasLogController : Controller
    {
        private readonly IGasLogManager _gasLogManager;
        private readonly IAutoEntityManager<AutomobileInfo> _autoManager;
        private readonly IAutoEntityManager<GasDiscount> _discountManager;
        private readonly IAutoEntityManager<GasStation> _stationManager;
        private readonly IMapper _mapper;
        private readonly IPropertyMappingService _propertyMappingService;
        private readonly IPropertyCheckService _propertyCheckService;
        private readonly ILogger<GasLogController> _logger;

        public GasLogController(
            IGasLogManager gasLogManager,
            IMapper mapper,
            IAutoEntityManager<AutomobileInfo> autoManager,
            IAutoEntityManager<GasDiscount> discountManager,
            IAutoEntityManager<GasStation> stationManager,
            IPropertyMappingService propertyMappingService,
            IPropertyCheckService propertyCheckService,
            ILogger<GasLogController> logger)
        {
            ArgumentNullException.ThrowIfNull(gasLogManager);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(autoManager);
            ArgumentNullException.ThrowIfNull(discountManager);
            ArgumentNullException.ThrowIfNull(stationManager);
            ArgumentNullException.ThrowIfNull(propertyMappingService);
            ArgumentNullException.ThrowIfNull(propertyCheckService);
            ArgumentNullException.ThrowIfNull(logger);

            _gasLogManager = gasLogManager;
            _mapper = mapper;
            _autoManager = autoManager;
            _discountManager = discountManager;
            _stationManager = stationManager;
            _propertyMappingService = propertyMappingService;
            _propertyCheckService = propertyCheckService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves gas log entries for a specific automobile.
        /// </summary>
        /// <param name="autoId">The automobile identifier.</param>
        /// <param name="resourceParameters">Pagination, sorting, and filtering parameters.</param>
        /// <returns>A list of gas log entries.</returns>
        [HttpGet(Name = "GetGasLogs")]
        [TypeFilter(typeof(GasLogsResultFilter))]
        [TypeFilter(typeof(CollectionResultFilter))]
        [ProducesResponseType(typeof(ApiEntityCollection<ApiGasLog>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Get(int autoId, [FromQuery] GasLogResourceParameters resourceParameters)
        {
            if (!string.IsNullOrEmpty(resourceParameters.OrderBy))
            {
                var validationResult = _propertyMappingService.ValidMappingExistsFor<ApiGasLog, GasLog>(resourceParameters.OrderBy);
                if (!validationResult.Success || !validationResult.Value)
                {
                    return BadRequest(new ApiBadRequestResponse(validationResult.ErrorMessage ?? "Invalid order by clause"));
                }
                var mapResult = _propertyMappingService.GetPropertyMapping<ApiGasLog, GasLog>();
                if (!mapResult.Success)
                {
                    return BadRequest(new ApiBadRequestResponse(mapResult.ErrorMessage));
                }
                resourceParameters.OrderBy = resourceParameters.OrderBy.GetMappedSortClause(mapResult.Value);
            }

            if (!string.IsNullOrEmpty(resourceParameters.Fields))
            {
                var checkResult = _propertyCheckService.TypeHasProperties<ApiGasLog>(resourceParameters.Fields);
                if (!checkResult.Success || !checkResult.Value)
                {
                    return BadRequest(new ApiBadRequestResponse(checkResult.ErrorMessage ?? "Invalid fields specified"));
                }
            }

            var result = await _gasLogManager.GetGasLogsAsync(autoId, resourceParameters);
            if (!result.Success)
            {
                _logger.LogWarning("Failed to get gas logs for auto {AutoId}: {Error}", autoId, result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return Ok(result.Value ?? new Hmm.Utility.Dal.Query.PageList<GasLog>(
                Array.Empty<GasLog>(), 0, 1, 20));
        }

        /// <summary>
        /// Retrieves a single gas log entry by identifier.
        /// </summary>
        /// <param name="autoId">The automobile identifier.</param>
        /// <param name="id">The gas log identifier.</param>
        /// <param name="fields">Comma-separated list of fields to include in the response.</param>
        /// <returns>The gas log entry matching the specified identifier.</returns>
        [HttpGet("{id:int}", Name = "GetGasLogById")]
        [TypeFilter(typeof(GasLogResultFilter))]
        [ProducesResponseType(typeof(ApiGasLog), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Get(int autoId, int id, string fields)
        {
            var checkResult = _propertyCheckService.TypeHasProperties<ApiGasLog>(fields);
            if (!checkResult.Success || !checkResult.Value)
            {
                return BadRequest(new ApiBadRequestResponse(checkResult.ErrorMessage ?? "Invalid fields specified"));
            }

            var result = await _gasLogManager.GetEntityByIdAsync(id);
            if (!result.Success)
            {
                if (result.IsNotFound)
                {
                    return NotFound();
                }
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            var gasLog = result.Value;
            if (gasLog.AutomobileId != autoId)
            {
                return NotFound();
            }

            return Ok(gasLog);
        }

        /// <summary>
        /// Creates a historical gas log entry (does not update meter reading).
        /// </summary>
        /// <param name="autoId">The automobile identifier.</param>
        /// <param name="apiGasLog">The gas log data for creation.</param>
        /// <returns>The newly created gas log entry.</returns>
        [HttpPost("historylog", Name = "AddHistoryGasLog")]
        [TypeFilter(typeof(GasLogResultFilter))]
        [ProducesResponseType(typeof(ApiGasLog), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> HistoryLog(int autoId, [FromBody] ApiGasLogForCreation apiGasLog)
        {
            if (apiGasLog == null)
            {
                _logger.LogDebug("Null gas log found");
                return BadRequest(new ApiBadRequestResponse("Null gas log found"));
            }

            try
            {
                var gasLog = _mapper.Map<GasLog>(apiGasLog);

                // Get automobile
                var carResult = await _autoManager.GetEntityByIdAsync(autoId);
                if (!carResult.Success)
                {
                    var errMsg = $"Cannot find automobile with id {autoId} from data source";
                    _logger.LogDebug(errMsg);
                    return BadRequest(new ApiBadRequestResponse(errMsg));
                }

                gasLog.AutomobileId = autoId;

                // Resolve gas station from StationId
                var stationResult = await GetGasStationAsync(apiGasLog.StationId);
                if (!stationResult.Success)
                {
                    return BadRequest(new ApiBadRequestResponse(stationResult.ErrorMessage));
                }

                gasLog.Station = stationResult.Value;

                // Get discounts for gas log
                var discountsResult = await GetGasDiscountsAsync(apiGasLog.DiscountInfos);
                if (!discountsResult.Success)
                {
                    return BadRequest(new ApiBadRequestResponse(discountsResult.ErrorMessage));
                }

                gasLog.Discounts = discountsResult.Value;

                var saveResult = await _gasLogManager.LogHistoryAsync(gasLog);
                if (!saveResult.Success)
                {
                    return BadRequest(new ApiBadRequestResponse(saveResult.ErrorMessage));
                }

                return Ok(saveResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating history gas log for auto {AutoId}", autoId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Creates a new gas log entry and updates the automobile's meter reading.
        /// </summary>
        /// <param name="autoId">The automobile identifier.</param>
        /// <param name="apiGasLog">The gas log data for creation.</param>
        /// <returns>The newly created gas log entry.</returns>
        [HttpPost(Name = "AddGasLog")]
        [TypeFilter(typeof(GasLogResultFilter))]
        [ProducesResponseType(typeof(ApiGasLog), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Post(int autoId, [FromBody] ApiGasLogForCreation apiGasLog)
        {
            if (apiGasLog == null)
            {
                _logger.LogDebug("Null gas log found");
                return BadRequest(new ApiBadRequestResponse("Null gas log found"));
            }

            try
            {
                var gasLog = _mapper.Map<GasLog>(apiGasLog);

                // Get automobile
                var carResult = await _autoManager.GetEntityByIdAsync(autoId);
                if (!carResult.Success)
                {
                    var errMsg = $"Cannot find automobile with id {autoId} from data source";
                    _logger.LogDebug(errMsg);
                    return BadRequest(new ApiBadRequestResponse(errMsg));
                }

                gasLog.AutomobileId = autoId;

                // Resolve gas station from StationId
                var stationResult = await GetGasStationAsync(apiGasLog.StationId);
                if (!stationResult.Success)
                {
                    return BadRequest(new ApiBadRequestResponse(stationResult.ErrorMessage));
                }

                gasLog.Station = stationResult.Value;

                // Get discounts for gas log
                var discountsResult = await GetGasDiscountsAsync(apiGasLog.DiscountInfos);
                if (!discountsResult.Success)
                {
                    return BadRequest(new ApiBadRequestResponse(discountsResult.ErrorMessage));
                }

                gasLog.Discounts = discountsResult.Value;

                var saveResult = await _gasLogManager.CreateAsync(gasLog);
                if (!saveResult.Success)
                {
                    return BadRequest(new ApiBadRequestResponse(saveResult.ErrorMessage));
                }

                return Ok(saveResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating gas log for auto {AutoId}", autoId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Updates an existing gas log entry.
        /// </summary>
        /// <param name="id">The gas log identifier.</param>
        /// <param name="apiGasLog">The updated gas log data.</param>
        /// <returns>No content on success.</returns>
        [HttpPut("{id:int}", Name = "UpdateGasLog")]
        [ProducesResponseType(typeof(ApiGasLog), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Put(int id, [FromBody] ApiGasLogForUpdate apiGasLog)
        {
            if (apiGasLog == null)
            {
                return BadRequest(new ApiBadRequestResponse("Null gas log found"));
            }

            var getResult = await _gasLogManager.GetEntityByIdAsync(id);
            if (!getResult.Success)
            {
                if (getResult.IsNotFound)
                {
                    return NotFound();
                }
                return BadRequest(new ApiBadRequestResponse(getResult.ErrorMessage));
            }

            var gasLog = getResult.Value;
            _mapper.Map(apiGasLog, gasLog);

            // Resolve gas station from StationId if provided
            if (apiGasLog.StationId.HasValue)
            {
                var stationResult = await GetGasStationAsync(apiGasLog.StationId);
                if (!stationResult.Success)
                {
                    return BadRequest(new ApiBadRequestResponse(stationResult.ErrorMessage));
                }

                gasLog.Station = stationResult.Value;
            }

            var updateResult = await _gasLogManager.UpdateAsync(gasLog);
            if (!updateResult.Success)
            {
                return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
            }

            var newApiLog = _mapper.Map<ApiGasLog>(updateResult.Value);
            return Ok(newApiLog);
        }

        /// <summary>
        /// Partially updates a gas log entry using a JSON Patch document.
        /// </summary>
        /// <param name="autoId">The automobile identifier.</param>
        /// <param name="id">The gas log identifier.</param>
        /// <param name="patchDoc">The JSON Patch document.</param>
        /// <returns>No content on success.</returns>
        [HttpPatch("{id:int}", Name = "PatchGasLog")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Patch(int autoId, int id, [FromBody] JsonPatchDocument<ApiGasLogForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var getResult = await _gasLogManager.GetEntityByIdAsync(id);
                if (!getResult.Success)
                {
                    if (getResult.IsNotFound)
                    {
                        return NotFound();
                    }
                    return BadRequest(new ApiBadRequestResponse(getResult.ErrorMessage));
                }

                var curGasLog = getResult.Value;
                if (curGasLog.AutomobileId != autoId)
                {
                    return NotFound();
                }

                var gasLog2Update = _mapper.Map<ApiGasLogForUpdate>(curGasLog);
                patchDoc.ApplyTo(gasLog2Update);
                _mapper.Map(gasLog2Update, curGasLog);

                var updateResult = await _gasLogManager.UpdateAsync(curGasLog);
                if (!updateResult.Success)
                {
                    return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error patching gas log {Id} for auto {AutoId}", id, autoId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Deletes a gas log entry.
        /// </summary>
        /// <param name="id">The gas log identifier.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{id:int}", Name = "DeleteGasLog")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Delete(int id)
        {
            var getResult = await _gasLogManager.GetEntityByIdAsync(id);
            if (!getResult.Success)
            {
                if (getResult.IsNotFound)
                {
                    return NotFound($"Cannot find gas log with id : {id}");
                }
                return BadRequest(new ApiBadRequestResponse(getResult.ErrorMessage));
            }

            var gasLog = getResult.Value;
            gasLog.IsDeleted = true;

            var updateResult = await _gasLogManager.UpdateAsync(gasLog);
            return updateResult.Success ? NoContent() : StatusCode(StatusCodes.Status500InternalServerError);
        }

        private async Task<(bool Success, GasStation Value, string ErrorMessage)> GetGasStationAsync(int? stationId)
        {
            if (!stationId.HasValue)
            {
                return (false, null, "Gas station is required");
            }

            var stationResult = await _stationManager.GetEntityByIdAsync(stationId.Value);
            if (!stationResult.Success)
            {
                return (false, null, $"Cannot find gas station with id {stationId.Value} from data source");
            }

            return (true, stationResult.Value, null);
        }

        private async Task<(bool Success, List<GasDiscountInfo> Value, string ErrorMessage)> GetGasDiscountsAsync(
            IEnumerable<ApiDiscountInfo> discountInfos)
        {
            var discounts = new List<GasDiscountInfo>();

            if (discountInfos == null)
            {
                return (true, discounts, null);
            }

            foreach (var disc in discountInfos)
            {
                var discountResult = await _discountManager.GetEntityByIdAsync(disc.DiscountId);
                if (!discountResult.Success)
                {
                    var errMsg = $"Cannot find discount information for discount with id {disc.DiscountId} from data source";
                    return (false, null, errMsg);
                }

                discounts.Add(new GasDiscountInfo
                {
                    Program = discountResult.Value,
                    Amount = new Money(disc.Amount)
                });
            }

            return (true, discounts, null);
        }
    }
}
