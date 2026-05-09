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
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Controllers
{
    /// <summary>
    /// Manages auto insurance policies attached to a specific automobile.
    /// </summary>
    [Authorize]
    [ApiController]
    [EnableCors("AllowCors")]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/automobiles/{autoId:int}/insurance-policies")]
    [Produces("application/json")]
    public class AutoInsurancePolicyController : Controller
    {
        private readonly IAutoInsurancePolicyManager _policyManager;
        private readonly IAutoEntityManager<AutomobileInfo> _autoManager;
        private readonly IMapper _mapper;
        private readonly ILogger<AutoInsurancePolicyController> _logger;

        public AutoInsurancePolicyController(
            IAutoInsurancePolicyManager policyManager,
            IAutoEntityManager<AutomobileInfo> autoManager,
            IMapper mapper,
            ILogger<AutoInsurancePolicyController> logger)
        {
            ArgumentNullException.ThrowIfNull(policyManager);
            ArgumentNullException.ThrowIfNull(autoManager);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(logger);

            _policyManager = policyManager;
            _autoManager = autoManager;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet(Name = "GetAutoInsurancePolicies")]
        [TypeFilter(typeof(AutoInsurancePoliciesResultFilter))]
        [TypeFilter(typeof(CollectionResultFilter))]
        [ProducesResponseType(typeof(ApiEntityCollection<ApiAutoInsurancePolicy>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> Get(int autoId, [FromQuery] ResourceCollectionParameters resourceParameters)
        {
            var result = await _policyManager.GetByAutomobileAsync(autoId, resourceParameters);
            if (!result.Success)
            {
                _logger.LogWarning("Failed to get insurance policies for auto {AutoId}: {Error}", autoId, result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return Ok(result.Value ?? new PageList<AutoInsurancePolicy>(Array.Empty<AutoInsurancePolicy>(), 0, 1, 20));
        }

        [HttpGet("active", Name = "GetActiveAutoInsurancePolicy")]
        [TypeFilter(typeof(AutoInsurancePolicyResultFilter))]
        [ProducesResponseType(typeof(ApiAutoInsurancePolicy), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetActive(int autoId)
        {
            var result = await _policyManager.GetActiveForAutomobileAsync(autoId);
            if (!result.Success)
            {
                return result.IsNotFound ? NotFound() : BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return Ok(result.Value);
        }

        [HttpGet("{id:int}", Name = "GetAutoInsurancePolicyById")]
        [TypeFilter(typeof(AutoInsurancePolicyResultFilter))]
        [ProducesResponseType(typeof(ApiAutoInsurancePolicy), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetById(int autoId, int id)
        {
            var result = await _policyManager.GetEntityByIdAsync(id);
            if (!result.Success)
            {
                return result.IsNotFound ? NotFound() : BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            var policy = result.Value;
            if (policy.AutomobileId != autoId)
            {
                return NotFound();
            }

            return Ok(policy);
        }

        [HttpPost(Name = "AddAutoInsurancePolicy")]
        [TypeFilter(typeof(AutoInsurancePolicyResultFilter))]
        [ProducesResponseType(typeof(ApiAutoInsurancePolicy), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Post(int autoId, [FromBody] ApiAutoInsurancePolicyForCreate apiPolicy)
        {
            if (apiPolicy == null)
            {
                return BadRequest(new ApiBadRequestResponse("Null policy payload"));
            }

            var carResult = await _autoManager.GetEntityByIdAsync(autoId);
            if (!carResult.Success)
            {
                return BadRequest(new ApiBadRequestResponse($"Cannot find automobile with id {autoId}"));
            }

            var policy = _mapper.Map<AutoInsurancePolicy>(apiPolicy);
            policy.AutomobileId = autoId;

            var saveResult = await _policyManager.CreateAsync(policy);
            if (!saveResult.Success)
            {
                return BadRequest(new ApiBadRequestResponse(saveResult.ErrorMessage));
            }

            return Ok(saveResult.Value);
        }

        [HttpPut("{id:int}", Name = "UpdateAutoInsurancePolicy")]
        [ProducesResponseType(typeof(ApiAutoInsurancePolicy), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Put(int autoId, int id, [FromBody] ApiAutoInsurancePolicyForUpdate apiPolicy)
        {
            if (apiPolicy == null)
            {
                return BadRequest(new ApiBadRequestResponse("Null policy payload"));
            }

            var existing = await _policyManager.GetEntityByIdAsync(id);
            if (!existing.Success)
            {
                return existing.IsNotFound ? NotFound() : BadRequest(new ApiBadRequestResponse(existing.ErrorMessage));
            }

            if (existing.Value.AutomobileId != autoId)
            {
                return NotFound();
            }

            var policy = existing.Value;
            _mapper.Map(apiPolicy, policy);

            var updateResult = await _policyManager.UpdateAsync(policy);
            if (!updateResult.Success)
            {
                return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
            }

            var dto = _mapper.Map<ApiAutoInsurancePolicy>(updateResult.Value);
            return Ok(dto);
        }

        [HttpPatch("{id:int}", Name = "PatchAutoInsurancePolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Patch(int autoId, int id, [FromBody] JsonPatchDocument<ApiAutoInsurancePolicyForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch document is null or id invalid"));
            }

            var existing = await _policyManager.GetEntityByIdAsync(id);
            if (!existing.Success)
            {
                return existing.IsNotFound ? NotFound() : BadRequest(new ApiBadRequestResponse(existing.ErrorMessage));
            }

            if (existing.Value.AutomobileId != autoId)
            {
                return NotFound();
            }

            var policy = existing.Value;
            var dto = _mapper.Map<ApiAutoInsurancePolicyForUpdate>(policy);
            patchDoc.ApplyTo(dto);
            _mapper.Map(dto, policy);

            var updateResult = await _policyManager.UpdateAsync(policy);
            if (!updateResult.Success)
            {
                return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
            }

            return NoContent();
        }

        [HttpDelete("{id:int}", Name = "DeleteAutoInsurancePolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(int autoId, int id)
        {
            var existing = await _policyManager.GetEntityByIdAsync(id);
            if (!existing.Success)
            {
                return existing.IsNotFound ? NotFound() : BadRequest(new ApiBadRequestResponse(existing.ErrorMessage));
            }

            if (existing.Value.AutomobileId != autoId)
            {
                return NotFound();
            }

            var policy = existing.Value;
            policy.IsDeleted = true;

            var updateResult = await _policyManager.UpdateAsync(policy);
            return updateResult.Success ? NoContent() : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
