using AutoMapper;
using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Filters;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
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

        public GasDiscountController(IAutoEntityManager<GasDiscount> discountManager, IMapper mapper)
        {
            Guard.Against<ArgumentNullException>(discountManager == null, nameof(discountManager));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));

            _discountManager = discountManager;
            _mapper = mapper;
        }

        // GET api/automobiles/gaslogs/discounts
        [HttpGet(Name = "GetGasDiscounts")]
        [GasDiscountsResultFilter]
        public async Task<ActionResult> Get()
        {
            var allDiscounts = await _discountManager.GetEntitiesAsync();
            var discounts = allDiscounts.ToList();
            if (!discounts.Any())
            {
                return NotFound();
            }

            return Ok(discounts);
        }

        // GET api/automobiles/gaslogs/discounts/1
        [HttpGet("{id:int}", Name = "GetGasDiscountById")]
        [GasDiscountResultFilter]
        public async Task<IActionResult> Get(int id)
        {
            var discount = await _discountManager.GetEntityByIdAsync(id);
            if (discount == null)
            {
                return NotFound();
            }

            return Ok(discount);
        }

        // post api/automobiles/gaslogs/discounts
        [HttpPost(Name = "AddGasDiscount")]
        [GasDiscountResultFilter]
        public async Task<ActionResult> Post(ApiDiscountForCreate apiDiscount)
        {
            var discount = _mapper.Map<GasDiscount>(apiDiscount);
            var newDiscount = await _discountManager.CreateAsync(discount);
            if (newDiscount == null)
            {
                return BadRequest("Cannot create discount");
            }

            return Ok(newDiscount);
        }

        // PUT api/automobiles/gaslogs/discount/5
        [HttpPut("{id:int}", Name = "UpdateGasDiscount")]
        public async Task<IActionResult> Put(int id, ApiDiscountForUpdate apiDiscount)
        {
            var curDiscount = await _discountManager.GetEntityByIdAsync(id);
            if (curDiscount == null)
            {
                return BadRequest(new ApiBadRequestResponse("Cannot find gas discount"));
            }
            _mapper.Map(apiDiscount, curDiscount);
            var newDiscount = await _discountManager.UpdateAsync(curDiscount);
            if (newDiscount == null)
            {
                return BadRequest(new ApiBadRequestResponse("Cannot update gas discount"));
            }

            return NoContent();
        }

        // PATCH api/automobiles/gaslogs/discounts/1
        [HttpPatch("{id:int}", Name = "PatchGasDiscount")]
        public async Task<ActionResult> Patch(int id, JsonPatchDocument<ApiDiscountForUpdate> patchDocument)
        {
            var discount = await _discountManager.GetEntityByIdAsync(id);
            if (discount == null)
            {
                return NotFound();
            }

            var discountToPatch = _mapper.Map<ApiDiscountForUpdate>(discount);
            patchDocument.ApplyTo(discountToPatch, ModelState);

            if (!TryValidateModel(discountToPatch))
            {
                return ValidationProblem(ModelState);
            }

            _mapper.Map(discountToPatch, discount);
            await _discountManager.UpdateAsync(discount);

            return NoContent();
        }

        // DELETE api/automobiles/gaslogs/discounts/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var discount = await _discountManager.GetEntityByIdAsync(id);
                if (discount == null)
                {
                    return BadRequest(new ApiBadRequestResponse($"Cannot find discount with id : {id}"));
                }

                discount.IsActive = false;
                var updatedDiscount = await _discountManager.UpdateAsync(discount);
                if (updatedDiscount == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }

                return Ok();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}