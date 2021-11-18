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
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Controllers
{
    [ApiController]
    [Route("api/automobiles")]
    public class AutomobileController : Controller
    {
        private readonly IAutoEntityManager<AutomobileInfo> _automobileManager;
        private readonly IMapper _mapper;

        public AutomobileController(IAutoEntityManager<AutomobileInfo> automobileManager, IMapper mapper, ILogger<AutomobileController> logger)
        {
            Guard.Against<ArgumentNullException>(automobileManager == null, nameof(automobileManager));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));
            Guard.Against<ArgumentNullException>(logger == null, nameof(logger));

            _automobileManager = automobileManager;
            _mapper = mapper;
        }

        // GET api/automobiles
        [HttpGet(Name = "GetAutomobiles")]
        [AutomobilesResultFilter]
        public async Task<IActionResult> GetMobiles()
        {
            var autos = await _automobileManager.GetEntitiesAsync();
            if (!autos.Any())
            {
                return NotFound();
            }

            return Ok(autos);
        }

        // GET api/automobiles/1
        [HttpGet("{id:int}", Name = "GetAutomobileById")]
        [HttpHead]
        [AutomobileResultFilter]
        public async Task<IActionResult> GetAutomobileById(int id)
        {
            var car = await _automobileManager.GetEntityByIdAsync(id);
            if (car == null)
            {
                return NotFound();
            }

            return Ok(car);
        }

        // POST api/automobiles
        [HttpPost(Name = "AddAutomobile")]
        [AutomobileResultFilter]
        public async Task<ActionResult> CreateAutomobile(ApiAutomobileForCreate apiCar)
        {
            var car = _mapper.Map<AutomobileInfo>(apiCar);
            var newCar = await _automobileManager.CreateAsync(car);

            if (newCar == null || !_automobileManager.ProcessResult.Success)
            {
                throw new Exception(_automobileManager.ProcessResult.GetWholeMessage());
            }

            return Ok(newCar);
        }

        // PUT api/automobiles/4
        [HttpPut("{id:int}", Name = "UpdateAutomobile")]
        public async Task<IActionResult> UpdateAutomobile(int id, [FromBody] ApiAutomobileForUpdate apiCar)
        {
            if (apiCar == null)
            {
                return NotFound();
            }

            var curCar = await _automobileManager.GetEntityByIdAsync(id);
            if (curCar == null)
            {
                return BadRequest(new ApiBadRequestResponse("Cannot find automobile"));
            }
            _mapper.Map(apiCar, curCar);
            var newCar = _automobileManager.Update(curCar);
            if (newCar == null)
            {
                return BadRequest(new ApiBadRequestResponse("Cannot update automobile"));
            }

            _mapper.Map<ApiAutomobile>(newCar);
            return NoContent();
        }

        // PATCH api/automobiles/4
        [HttpPatch("{id:int}", Name = "PatchAutomobile")]
        public async Task<IActionResult> Patch(int id, [FromBody] JsonPatchDocument<ApiAutomobileForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var curAutomobile = await _automobileManager.GetEntityByIdAsync(id);
                if (curAutomobile == null)
                {
                    return NotFound();
                }

                var automobile2Update = _mapper.Map<ApiAutomobileForUpdate>(curAutomobile);
                patchDoc.ApplyTo(automobile2Update);
                _mapper.Map(automobile2Update, curAutomobile);

                var newAutomobile = await _automobileManager.UpdateAsync(curAutomobile);
                if (newAutomobile == null)
                {
                    return BadRequest(_automobileManager.ProcessResult.MessageList);
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // DELETE api/automobiles/5
        [HttpDelete("{id:int}", Name = "DeleteAutomobile")]
        public IActionResult Delete(int id)
        {
            throw new NotImplementedException();
        }
    }
}