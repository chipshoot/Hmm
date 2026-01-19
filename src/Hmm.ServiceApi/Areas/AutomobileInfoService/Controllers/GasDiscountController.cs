using AutoMapper;
using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Filters;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/automobiles/gaslogs/discounts")]
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

        // GET api/automobiles/gaslogs/discounts
        [HttpGet(Name = "GetGasDiscounts")]
        [GasDiscountsResultFilter]
        [CollectionResultFilter]
        public async Task<ActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var result = await _discountManager.GetEntitiesAsync(resourceCollectionParameters);
            if (!result.Success)
            {
                _logger.LogWarning("Failed to get gas discounts: {Error}", result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            if (result.Value == null || result.Value.Count == 0)
            {
                return NotFound();
            }

            return Ok(result.Value);
        }

        // GET api/automobiles/gaslogs/discounts/1
        [HttpGet("{id:int}", Name = "GetGasDiscountById")]
        [GasDiscountResultFilter]
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

        // POST api/automobiles/gaslogs/discounts
        [HttpPost(Name = "AddGasDiscount")]
        [GasDiscountResultFilter]
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

        // PUT api/automobiles/gaslogs/discount/5
        [HttpPut("{id:int}", Name = "UpdateGasDiscount")]
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

        // PATCH api/automobiles/gaslogs/discounts/1
        [HttpPatch("{id:int}", Name = "PatchGasDiscount")]
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

        // DELETE api/automobiles/gaslogs/discounts/1
        [HttpDelete("{id:int}")]
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
