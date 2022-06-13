using AutoMapper;
using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Filters;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Models;
using Hmm.ServiceApi.Models.Validation;
using Hmm.Utility.Currency;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Infrastructure;
using Microsoft.AspNetCore.Cors;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Controllers
{
    [ApiController]
    [EnableCors("AllowCors")]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/automobiles/{autoId:int}/gaslogs")]
    [ValidationModel]
    public class GasLogController : Controller
    {
        private readonly IGasLogManager _gasLogManager;
        private readonly IAutoEntityManager<AutomobileInfo> _autoManager;
        private readonly IAutoEntityManager<GasDiscount> _discountManager;
        private readonly IMapper _mapper;

        public GasLogController(IGasLogManager gasLogManager, IMapper mapper,
            IAutoEntityManager<AutomobileInfo> autoManager,
            IAutoEntityManager<GasDiscount> discountManager)
        {
            Guard.Against<ArgumentNullException>(gasLogManager == null, nameof(gasLogManager));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));
            Guard.Against<ArgumentNullException>(autoManager == null, nameof(autoManager));
            Guard.Against<ArgumentNullException>(discountManager == null, nameof(discountManager));

            _gasLogManager = gasLogManager;
            _mapper = mapper;
            _autoManager = autoManager;
            _discountManager = discountManager;
        }

        // GET api/automobiles/1/gaslogs
        [HttpGet(Name = "GetGasLogs")]
        [GasLogsResultFilter]
        public async Task<ActionResult> Get([FromQuery] GasLogResourceParameters gasLogResourceParameters)
        {
            //var userId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            //if (userId == null)
            //{
            //    var errorMsg = "Cannot get author information with null user id";
            //    Log.Error(errorMsg);
            //    return BadRequest(errorMsg);
            //}

            //var user = _userManager.GetEntities().FirstOrDefault(u => u.Id == Guid.Parse(userId));
            //if (user == null)
            //{
            //    return OK(new List<ApiGasLog>());
            //}

            var gasLogs = await _gasLogManager.GetEntitiesAsync(gasLogResourceParameters);
            if (!gasLogs.Any())
            {
                return NotFound();
            }

            return Ok(gasLogs);
        }

        // GET api/automobiles/1/gaslogs/5
        [HttpGet("{id:int}", Name = "GetGasLogById")]
        [GasLogResultFilter]
        public async Task<ActionResult> Get(int autoId, int id)
        {
            var gasLog = await _gasLogManager.GetEntityByIdAsync(id);
            if (gasLog == null || gasLog.Car.Id != autoId)
            {
                return NotFound();
            }

            return Ok(gasLog);
        }

        // POST api/automobiles/1/gaslogs/historyLog
        [HttpPost("historylog", Name = "AddHistoryGasLog")]
        [GasLogResultFilter]
        public async Task<ActionResult> HistoryLog(int autoId, [FromBody] ApiGasLogForCreation apiGasLog)
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
                var car = await _autoManager.GetEntityByIdAsync(autoId);
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
                        var discount = await _discountManager.GetEntityByIdAsync(disc.DiscountId);
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

                var savedGasLog = await _gasLogManager.LogHistoryAsync(gasLog);
                if (savedGasLog == null)
                {
                    var errMsg = _gasLogManager.ProcessResult.GetWholeMessage();
                    return BadRequest(new ApiBadRequestResponse(errMsg));
                }
                return Ok(savedGasLog);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // POST api/automobiles/1/gaslogs
        [HttpPost(Name = "AddGasLog")]
        [GasLogResultFilter]
        public async Task<ActionResult> Post(int autoId, [FromBody] ApiGasLogForCreation apiGasLog)
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
                var car = await _autoManager.GetEntityByIdAsync(autoId);
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
                        var discount = await _discountManager.GetEntityByIdAsync(disc.DiscountId);
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

                var savedGasLog = await _gasLogManager.CreateAsync(gasLog);
                if (savedGasLog == null)
                {
                    var error = _gasLogManager.ProcessResult.GetWholeMessage();
                    return BadRequest(new ApiBadRequestResponse(error));
                }
                return Ok(savedGasLog);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT api/automobiles/1/gaslogs/5
        [HttpPut("{id:int}", Name = "UpdateGasLog")]
        public async Task<ActionResult> Put(int id, [FromBody] ApiGasLogForUpdate apiGasLog)
        {
            if (apiGasLog == null)
            {
                return BadRequest(new ApiBadRequestResponse("null gas log found"));
            }

            var gasLog = await _gasLogManager.GetEntityByIdAsync(id);
            if (gasLog == null)
            {
                return BadRequest(new ApiBadRequestResponse($"Cannot find gas log with id {id}"));
            }

            _mapper.Map(apiGasLog, gasLog);
            var newLog = await _gasLogManager.UpdateAsync(gasLog);
            if (newLog == null)
            {
                return BadRequest(new ApiBadRequestResponse($"Cannot update gas log with id {id}"));
            }

            var newApiLog = _mapper.Map<ApiGasLog>(newLog);
            return Ok(newApiLog);
        }

        // PATCH api/automobiles/1/gaslogs/4
        [HttpPatch("{id:int}", Name = "PatchGasLog")]
        public async Task<ActionResult> Patch(int autoId, int id, [FromBody] JsonPatchDocument<ApiGasLogForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var curGasLog = await _gasLogManager.GetEntityByIdAsync(id);
                if (curGasLog == null || curGasLog.Car.Id != autoId)
                {
                    return NotFound();
                }

                var gasLog2Update = _mapper.Map<ApiGasLogForUpdate>(curGasLog);
                patchDoc.ApplyTo(gasLog2Update);
                _mapper.Map(gasLog2Update, curGasLog);

                var newGasLog = await _gasLogManager.UpdateAsync(curGasLog);
                if (newGasLog == null)
                {
                    return BadRequest(_gasLogManager.ProcessResult.MessageList);
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // DELETE api/automobiles/1/gaslogs/5
        [HttpDelete("{id:int}", Name = "DeleteGasLog")]
        public async Task<ActionResult> Delete(int id)
        {
            var gasLog = await _gasLogManager.GetEntityByIdAsync(id);
            if (gasLog == null)
            {
                return NotFound($"Cannot find gas log with id : {id}");

            }

            gasLog.IsDeleted = true;
            await _gasLogManager.UpdateAsync(gasLog);
            return _gasLogManager.ProcessResult.Success ? NoContent() : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}