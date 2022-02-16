using AutoMapper;
using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.DtoEntity;
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
using Hmm.ServiceApi.Areas.AutomobileInfoService.Filters;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/automobiles/{autoId:int}/gaslogs")]
    [ValidationModel]
    public class GasLogController : Controller
    {
        private readonly IAutoEntityManager<GasLog> _gasLogManager;

        private readonly IAutoEntityManager<AutomobileInfo> _autoManager;
        private readonly IAutoEntityManager<GasDiscount> _discountManager;
        private readonly IMapper _mapper;

        public GasLogController(IAutoEntityManager<GasLog> gasLogManager, IMapper mapper,
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
        public IActionResult Get(int autoId)
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
            //    return Ok(new List<ApiGasLog>());
            //}

            var gasLogs = _gasLogManager.GetEntities().Where(l=>l.Car.Id == autoId).ToList();
            if (!gasLogs.Any())
            {
                return NotFound();
            }

            return Ok(gasLogs);
        }

        // GET api/automobiles/1/gaslogs/5
        [HttpGet("{id:int}", Name = "GetGasLogById")]
        [GasLogResultFilter]
        public IActionResult Get(int autoId, int id)
        {
            var gasLog = _gasLogManager.GetEntityById(id);
            if (gasLog == null || gasLog.Car.Id != autoId)
            {
                return NotFound();
            }

            return Ok(gasLog);
        }

        // POST api/automobiles/1/gaslogs
        [HttpPost(Name = "AddGasLog")]
        [GasLogResultFilter]
        public IActionResult Post(int autoId, [FromBody] ApiGasLogForCreation apiGasLog)
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
                var car = _autoManager.GetEntityById(autoId);
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
                return Ok(savedGasLog);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT api/automobiles/1/gaslogs/5
        [HttpPut("{id:int}", Name = "UpdateGasLog")]
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

        // PATCH api/automobiles/1/gaslogs/4
        [HttpPatch("{id:int}", Name = "PatchGasLog")]
        public IActionResult Patch(int autoId, int id, [FromBody] JsonPatchDocument<ApiGasLogForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var curGasLog = _gasLogManager.GetEntities().FirstOrDefault(r => r.Id == id && r.Car.Id == autoId);
                if (curGasLog == null)
                {
                    return NotFound();
                }

                var gasLog2Update = _mapper.Map<ApiGasLogForUpdate>(curGasLog);
                patchDoc.ApplyTo(gasLog2Update);
                _mapper.Map(gasLog2Update, curGasLog);

                var newGasLog = _gasLogManager.Update(curGasLog);
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
        public IActionResult Delete(int autoId, int id)
        {
            return StatusCode(StatusCodes.Status405MethodNotAllowed);
        }

        private IEnumerable<Link> CreateLinksForGasLog(int autoId, int logId)
        {
            var links = new List<Link>
            {
                // self
                new()
                {
                    Title = "self",
                    Rel = "self",
                    Href = Url.Link("GetGasLogById", new { autoId, id = logId }),
                    Method = "Get"
                },
                new()
                {
                    Title = "AddGasLog",
                    Rel = "create_gasLog",
                    Href = Url.Link("AddGasLog", new { autoId}),
                    Method = "POST"
                },
                new()
                {
                    Title = "UpdateGasLog",
                    Rel = "update_gasLog",
                    Href = Url.Link("UpdateGasLog", new {autoId, id = logId }),
                    Method = "PUT"
                },
                new()
                {
                    Title = "PatchGasLog",
                    Rel = "patch_gasLog",
                    Href = Url.Link("PatchGasLog", new {autoId, id = logId }),
                    Method = "PATCH"
                }
            };

            return links;
        }
    }
}