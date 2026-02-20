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
    /// Manages gas discount program CRUD operations.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/api/v{version:apiVersion}/automobiles/gaslogs/discounts")]
    [Produces("application/json")]
    public class GasDiscountController : Controller
    {
        private readonly IAutoEntityManager<GasDiscount> _discountManager;
        private readonly IMapper _mapper;
        private readonly ILogger<GasDiscountController> _logger;

        public GasDiscountController(
            IAutoEntityManager<GasDiscount> discountManager,
            IMapper mapper,
            ILogger<GasDiscountController> logger)
        {
            ArgumentNullException.ThrowIfNull(discountManager);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(logger);

            _discountManager = discountManager;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a paginated list of gas discount programs.
        /// </summary>
        /// <param name="resourceCollectionParameters">Pagination and sorting parameters.</param>
        /// <returns>A paginated list of gas discounts.</returns>
        [HttpGet(Name = "GetGasDiscounts")]
        [TypeFilter(typeof(GasDiscountsResultFilter))]
        [TypeFilter(typeof(CollectionResultFilter))]
        [ProducesResponseType(typeof(ApiEntityCollection<ApiDiscount>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var result = await _discountManager.GetEntitiesAsync(resourceCollectionParameters);
            if (!result.Success)
            {
                _logger.LogWarning("Failed to get gas discounts: {Error}", result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return Ok(result.Value ?? new Hmm.Utility.Dal.Query.PageList<GasDiscount>(
                Array.Empty<GasDiscount>(), 0, 1, 10));
        }

        /// <summary>
        /// Retrieves a single gas discount by identifier.
        /// </summary>
        /// <param name="id">The gas discount identifier.</param>
        /// <returns>The gas discount matching the specified identifier.</returns>
        [HttpGet("{id:int}", Name = "GetGasDiscountById")]
        [TypeFilter(typeof(GasDiscountResultFilter))]
        [ProducesResponseType(typeof(ApiDiscount), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _discountManager.GetEntityByIdAsync(id);
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
        /// Creates a new gas discount program.
        /// </summary>
        /// <param name="apiDiscount">The gas discount data for creation.</param>
        /// <returns>The newly created gas discount.</returns>
        [HttpPost(Name = "AddGasDiscount")]
        [TypeFilter(typeof(GasDiscountResultFilter))]
        [ProducesResponseType(typeof(ApiDiscount), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> Post(ApiDiscountForCreate apiDiscount)
        {
            var discount = _mapper.Map<GasDiscount>(apiDiscount);
            var result = await _discountManager.CreateAsync(discount);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to create gas discount: {Error}", result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Updates an existing gas discount program.
        /// </summary>
        /// <param name="id">The gas discount identifier.</param>
        /// <param name="apiDiscount">The updated gas discount data.</param>
        /// <returns>No content on success.</returns>
        [HttpPut("{id:int}", Name = "UpdateGasDiscount")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Put(int id, ApiDiscountForUpdate apiDiscount)
        {
            if (apiDiscount == null)
            {
                return BadRequest(new ApiBadRequestResponse("Discount data is required"));
            }

            var getResult = await _discountManager.GetEntityByIdAsync(id);
            if (!getResult.Success)
            {
                if (getResult.IsNotFound)
                {
                    return NotFound();
                }
                return BadRequest(new ApiBadRequestResponse(getResult.ErrorMessage));
            }

            var curDiscount = getResult.Value;
            _mapper.Map(apiDiscount, curDiscount);

            var updateResult = await _discountManager.UpdateAsync(curDiscount);
            if (!updateResult.Success)
            {
                return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
            }

            return NoContent();
        }

        /// <summary>
        /// Partially updates a gas discount using a JSON Patch document.
        /// </summary>
        /// <param name="id">The gas discount identifier.</param>
        /// <param name="patchDocument">The JSON Patch document.</param>
        /// <returns>No content on success.</returns>
        [HttpPatch("{id:int}", Name = "PatchGasDiscount")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Patch(int id, JsonPatchDocument<ApiDiscountForUpdate> patchDocument)
        {
            if (patchDocument == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            var getResult = await _discountManager.GetEntityByIdAsync(id);
            if (!getResult.Success)
            {
                if (getResult.IsNotFound)
                {
                    return NotFound();
                }
                return BadRequest(new ApiBadRequestResponse(getResult.ErrorMessage));
            }

            var discount = getResult.Value;
            var discountToPatch = _mapper.Map<ApiDiscountForUpdate>(discount);
            patchDocument.ApplyTo(discountToPatch, ModelState);

            if (!TryValidateModel(discountToPatch))
            {
                return ValidationProblem(ModelState);
            }

            _mapper.Map(discountToPatch, discount);
            var updateResult = await _discountManager.UpdateAsync(discount);

            if (!updateResult.Success)
            {
                return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
            }

            return NoContent();
        }

        /// <summary>
        /// Deletes a gas discount program.
        /// </summary>
        /// <param name="id">The gas discount identifier.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var getResult = await _discountManager.GetEntityByIdAsync(id);
                if (!getResult.Success)
                {
                    if (getResult.IsNotFound)
                    {
                        return NotFound();
                    }
                    return BadRequest(new ApiBadRequestResponse(getResult.ErrorMessage));
                }

                var discount = getResult.Value;
                discount.IsActive = false;

                var updateResult = await _discountManager.UpdateAsync(discount);
                if (!updateResult.Success)
                {
                    _logger.LogError("Failed to deactivate discount {Id}: {Error}", id, updateResult.ErrorMessage);
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting discount {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
