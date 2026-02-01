using AutoMapper;
using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Filters;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Cors;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Controllers
{
    [Authorize]
    [ApiController]
    [EnableCors("AllowCors")]
    [ApiVersion("1.0")]
    [Route("/api/v{version:apiVersion}/automobiles")]
    [Produces("application/json")]
    public class AutomobileController : Controller
    {
        private readonly IAutoEntityManager<AutomobileInfo> _automobileManager;
        private readonly IMapper _mapper;
        private readonly ILogger<AutomobileController> _logger;

        public AutomobileController(
            IAutoEntityManager<AutomobileInfo> automobileManager,
            IMapper mapper,
            ILogger<AutomobileController> logger)
        {
            ArgumentNullException.ThrowIfNull(automobileManager);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(logger);

            _automobileManager = automobileManager;
            _mapper = mapper;
            _logger = logger;
        }

        // GET api/automobiles
        [HttpGet(Name = "GetAutomobiles")]
        [TypeFilter(typeof(AutomobilesResultFilter))]
        [TypeFilter(typeof(CollectionResultFilter))]
        [ProducesResponseType(typeof(ApiEntityCollection<ApiAutomobile>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMobiles([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var result = await _automobileManager.GetEntitiesAsync(resourceCollectionParameters);
            if (!result.Success)
            {
                _logger.LogWarning("Failed to get automobiles: {Error}", result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            if (result.Value == null || result.Value.Count == 0)
            {
                return NotFound();
            }

            return Ok(result.Value);
        }

        // GET api/automobiles/1
        [HttpGet("{id:int}", Name = "GetAutomobileById")]
        [HttpHead]
        [TypeFilter(typeof(AutomobileResultFilter))]
        [ProducesResponseType(typeof(ApiAutomobile), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAutomobileById(int id)
        {
            var result = await _automobileManager.GetEntityByIdAsync(id);
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

        // POST api/automobiles
        [HttpPost(Name = "AddAutomobile")]
        [TypeFilter(typeof(AutomobileResultFilter))]
        [ProducesResponseType(typeof(ApiAutomobile), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> CreateAutomobile(ApiAutomobileForCreate apiCar)
        {
            var car = _mapper.Map<AutomobileInfo>(apiCar);
            var result = await _automobileManager.CreateAsync(car);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to create automobile: {Error}", result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return Ok(result.Value);
        }

        // PUT api/automobiles/4
        [HttpPut("{id:int}", Name = "UpdateAutomobile")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAutomobile(int id, [FromBody] ApiAutomobileForUpdate apiCar)
        {
            if (apiCar == null)
            {
                return BadRequest(new ApiBadRequestResponse("Automobile data is required"));
            }

            var getResult = await _automobileManager.GetEntityByIdAsync(id);
            if (!getResult.Success)
            {
                if (getResult.IsNotFound)
                {
                    return NotFound();
                }
                return BadRequest(new ApiBadRequestResponse(getResult.ErrorMessage));
            }

            var curCar = getResult.Value;
            _mapper.Map(apiCar, curCar);

            var updateResult = await _automobileManager.UpdateAsync(curCar);
            if (!updateResult.Success)
            {
                return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
            }

            return NoContent();
        }

        // PATCH api/automobiles/4
        [HttpPatch("{id:int}", Name = "PatchAutomobile")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Patch(int id, [FromBody] JsonPatchDocument<ApiAutomobileForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var getResult = await _automobileManager.GetEntityByIdAsync(id);
                if (!getResult.Success)
                {
                    if (getResult.IsNotFound)
                    {
                        return NotFound();
                    }
                    return BadRequest(new ApiBadRequestResponse(getResult.ErrorMessage));
                }

                var curAutomobile = getResult.Value;
                var automobile2Update = _mapper.Map<ApiAutomobileForUpdate>(curAutomobile);
                patchDoc.ApplyTo(automobile2Update);
                _mapper.Map(automobile2Update, curAutomobile);

                var updateResult = await _automobileManager.UpdateAsync(curAutomobile);
                if (!updateResult.Success)
                {
                    return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error patching automobile {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // DELETE api/automobiles/5
        [HttpDelete("{id:int}", Name = "DeleteAutomobile")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status501NotImplemented)]
        public IActionResult Delete(int id)
        {
            throw new NotImplementedException();
        }
    }
}
