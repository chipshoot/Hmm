using AutoMapper;
using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

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

        // GET api/automobiles/5
        [HttpGet("{id}", Name = "GetAutomobile")]
        [HttpHead]
        public IActionResult GetAutomobileById(int id)
        {
            var car = _automobileManager.GetEntityById(id);
            if (car == null)
            {
                return NotFound();
            }

            var apiCar = _mapper.Map<ApiAutomobile>(car);
            return Ok(apiCar);
        }

        // GET api/automobiles/
        [HttpGet]
        public ActionResult<IEnumerable<ApiAutomobile>> GetMobiles()
        {
            var apiCars = _mapper.Map<IEnumerable<ApiAutomobile>>(_automobileManager.GetEntities().ToList());
            return Ok(apiCars);
        }

        // POST api/automobiles
        [HttpPost]
        public ActionResult CreateAutomobile(ApiAutomobileForCreate apiCar)
        {
            var newApiCars = new List<ApiAutomobile>();
            var car = _mapper.Map<AutomobileInfo>(apiCar);
            var newCar = _automobileManager.Create(car);

            if (newCar == null || !_automobileManager.ProcessResult.Success)
            {
                throw new Exception(_automobileManager.ProcessResult.GetWholeMessage());
            }

            return Ok();
        }

        // PUT api/automobiles/5
        [HttpPut("{id}")]
        public IActionResult UpdateAutomobile(int id, [FromBody] ApiAutomobile apiCar)
        {
            if (apiCar == null)
            {
                return NotFound();
            }

            var curCar = _automobileManager.GetEntityById(id);
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

        // DELETE api/automobiles/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            throw new NotImplementedException();
        }
    }
}