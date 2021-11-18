using AutoMapper;
using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.DtoEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Controllers
{
    [ApiController]
    [Route("api/automobiles/gaslogs/discounts")]
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
        public ActionResult<IEnumerable<ApiDiscount>> GetDiscounts()
        {
            var discounts = _mapper.Map<IEnumerable<ApiDiscount>>(_discountManager.GetEntities()).ToList();
            if (!discounts.Any())
            {
                return NotFound();
            }

            foreach (var discount in discounts)
            {
                discount.Links = CreateLinksForDiscount(discount.Id);
            }
            return Ok(discounts);
        }

        // GET api/automobiles/gaslogs/discounts/1
        [HttpGet("{id:int}", Name = "GetGasDiscountById")]
        public IActionResult GetDiscountById(int id)
        {
            var discount = _discountManager.GetEntityById(id);
            if (discount == null)
            {
                return NotFound();
            }

            var apiDiscount = _mapper.Map<ApiDiscount>(discount);
            apiDiscount.Links = CreateLinksForDiscount(apiDiscount.Id);
            return Ok(apiDiscount);
        }

        // post api/automobiles/gaslogs/discounts
        [HttpPost(Name = "AddGasDiscount")]
        public ActionResult<ApiDiscount> CreateGasDiscount(ApiDiscountForCreate apiDiscount)
        {
            var discount = _mapper.Map<GasDiscount>(apiDiscount);
            var newDiscount = _discountManager.Create(discount);
            if (newDiscount == null)
            {
                return BadRequest("Cannot create discount");
            }

            var discountToReturn = _mapper.Map<ApiDiscount>(newDiscount);
            discountToReturn.Links = CreateLinksForDiscount(discountToReturn.Id);
            return Ok(discountToReturn);
        }

        // PUT api/automobiles/gaslogs/discount/5
        [HttpPut("{id:int}", Name = "UpdateGasDiscount")]
        public IActionResult UpdateDiscount(int id, ApiDiscountForUpdate apiDiscount)
        {
            var curDiscount = _discountManager.GetEntityById(id);
            if (curDiscount == null)
            {
                return BadRequest(new ApiBadRequestResponse("Cannot find gas discount"));
            }
            _mapper.Map(apiDiscount, curDiscount);
            var newDiscount = _discountManager.Update(curDiscount);
            if (newDiscount == null)
            {
                return BadRequest(new ApiBadRequestResponse("Cannot update gas discount"));
            }

            return NoContent();
        }

        // PATCH api/automobiles/gaslogs/discounts/1
        [HttpPatch("{id:int}", Name = "PatchGasDiscount")]
        public ActionResult PartialUpdateDiscount(int id, JsonPatchDocument<ApiDiscountForUpdate> patchDocument)
        {
            var discount = _discountManager.GetEntityById(id);
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
            _discountManager.Update(discount);

            return NoContent();
        }

        // DELETE api/automobiles/gaslogs/discounts/1
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var discount = _discountManager.GetEntityById(id);
                if (discount == null)
                {
                    return BadRequest(new ApiBadRequestResponse($"Cannot find discount with id : {id}"));
                }

                discount.IsActive = false;
                var updatedDiscount = _discountManager.Update(discount);
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

        private IEnumerable<Link> CreateLinksForDiscount(int discountId)
        {
            var links = new List<Link>
            {
                // self
                new()
                {
                    Title = "self",
                    Rel = "self",
                    Href = Url.Link("GetGasDiscountById", new { id = discountId }),
                    Method = "Get"
                },
                new()
                {
                    Title = "AddGasDiscount",
                    Rel = "create_gasDiscount",
                    Href = Url.Link("AddGasDiscount", null),
                    Method = "POST"
                },
                new()
                {
                    Title = "UpdateGasDiscount",
                    Rel = "update_gasDiscount",
                    Href = Url.Link("UpdateGasDiscount", new { id = discountId }),
                    Method = "PUT"
                },
                new()
                {
                    Title = "PatchGasDiscount",
                    Rel = "patch_gasDiscount",
                    Href = Url.Link("PatchGasDiscount", new { id = discountId }),
                    Method = "PATCH"
                }
            };

            return links;
        }
    }
}