using AutoMapper;
using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Models;
using Hmm.ServiceApi.Models.Validation;
using Hmm.Utility.Currency;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Controllers
{
    public class GaslogController : Controller
    {
        [Route("api/automobiles/gaslogs")]
        [ValidationModel]
        public class GasLogController : Controller
        {
            private readonly IAutoEntityManager<GasLog> _gasLogManager;
            private readonly IAutoEntityManager<AutomobileInfo> _autoManager;
            private readonly IAutoEntityManager<GasDiscount> _discountManager;
            private readonly IMapper _mapper;
            private readonly IAuthorManager _userManager;

            public GasLogController(IAutoEntityManager<GasLog> gasLogManager, IMapper mapper,
                IAuthorManager userManager, IAutoEntityManager<AutomobileInfo> autoManager,
                IAutoEntityManager<GasDiscount> discountManager)
            {
                Guard.Against<ArgumentNullException>(gasLogManager == null, nameof(gasLogManager));
                Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));
                Guard.Against<ArgumentNullException>(userManager == null, nameof(userManager));
                Guard.Against<ArgumentNullException>(autoManager == null, nameof(autoManager));
                Guard.Against<ArgumentNullException>(discountManager == null, nameof(discountManager));

                _gasLogManager = gasLogManager;
                _mapper = mapper;
                _userManager = userManager;
                _autoManager = autoManager;
                _discountManager = discountManager;
            }

            // GET api/automobiles/gaslogs
            [HttpGet]
            public IActionResult Get()
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                if (userId == null)
                {
                    var errorMsg = "Cannot get author information with null user id";
                    Log.Error(errorMsg);
                    return BadRequest(errorMsg);
                }

                var user = _userManager.GetEntities().FirstOrDefault(u => u.Id == Guid.Parse(userId));
                if (user == null)
                {
                    return Ok(new List<ApiGasLog>());
                }

                var gasLogs = _gasLogManager.GetEntities().ToList();
                var apiGasLogs = _mapper.Map<List<ApiGasLog>>(gasLogs);
                return Ok(apiGasLogs);
            }

            // GET api/automobiles/gaslogs/5
            [HttpGet("{id}")]
            public IActionResult Get(int id)
            {
                var gasLog = _gasLogManager.GetEntityById(id);
                if (gasLog == null)
                {
                    return NotFound();
                }

                var apiGasLog = _mapper.Map<ApiGasLog>(gasLog);
                return Ok(apiGasLog);
            }

            // POST api/automobiles/gaslogs
            [HttpPost]
            public IActionResult Post([FromBody] ApiGasLogForCreation apiGasLog)
            {
                if (apiGasLog == null)
                {
                    var errMsg = "null gas log found";
                    Log.Logger.Debug(errMsg);
                    return BadRequest(new ApiBadRequestResponse(errMsg));
                }

                try
                {
                    var gasLog = _mapper.Map<GasLog>(apiGasLog);

                    // get automobile
                    var car = _autoManager.GetEntityById(apiGasLog.AutomobileId);
                    if (car == null)
                    {
                        var errMsg = $"Cannot find automobile with id {apiGasLog.AutomobileId} from data source";
                        Log.Logger.Debug(errMsg);
                        return BadRequest(new ApiBadRequestResponse(errMsg));
                    }

                    gasLog.Car = car;

                    // get discount for gas log
                    var discounts = new List<GasDiscountInfo>();
                    if (apiGasLog.DiscountInfos != null)
                    {
                        foreach (var disc in apiGasLog.DiscountInfos)
                        {
                            var discount = _discountManager.GetEntityById(disc.DiscountId);
                            if (discount == null)
                            {
                                var errMsg =
                                    $"Cannot find discount information for discount with id {disc.DiscountId} from data source";
                                return BadRequest(new ApiBadRequestResponse(errMsg));
                            }

                            discounts.Add(new GasDiscountInfo
                            {
                                Program = discount,
                                Amount = new Money(disc.Amount)
                            });
                        }
                    }

                    gasLog.Discounts = discounts;

                    var savedGasLog = _gasLogManager.Create(gasLog);
                    if (savedGasLog == null)
                    {
                        return BadRequest(new ApiBadRequestResponse("Cannot add gas log"));
                    }

                    var apiSavedGasLog = _mapper.Map<ApiGasLog>(savedGasLog);
                    return Ok(new ApiOkResponse(apiSavedGasLog));
                }
                catch (Exception)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }

            // PUT api/automobiles/gaslogs/5
            [HttpPut("{id}")]
            public IActionResult Put(int id, [FromBody] ApiGasLogForUpdate apiGasLog)
            {
                if (apiGasLog == null)
                {
                    return BadRequest(new ApiBadRequestResponse("null gas log found"));
                }

                var gasLog = _gasLogManager.GetEntityById(id);
                if (gasLog == null)
                {
                    return BadRequest(new ApiBadRequestResponse($"Cannot find gas log with id {id}"));
                }

                _mapper.Map(apiGasLog, gasLog);
                var newLog = _gasLogManager.Update(gasLog);
                if (newLog == null)
                {
                    return BadRequest(new ApiBadRequestResponse($"Cannot update gas log with id {id}"));
                }

                var newApiLog = _mapper.Map<ApiGasLog>(newLog);
                return Ok(newApiLog);
            }

            // DELETE api/automobiles/gaslogs/5
            [HttpDelete("{id}")]
            public IActionResult Delete(int id)
            {
                throw new NotImplementedException();
            }
        }
    }
}