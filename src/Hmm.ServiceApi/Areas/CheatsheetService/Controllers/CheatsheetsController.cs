using System;
using System.Threading.Tasks;
using AutoMapper;
using Hmm.Cheatsheet;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.ServiceApi.Areas.CheatsheetService.Filters;
using Hmm.ServiceApi.DtoEntity;
using Hmm.ServiceApi.DtoEntity.Cheatsheets;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Hmm.ServiceApi.Areas.CheatsheetService.Controllers
{
    /// <summary>
    /// Manages cheatsheet wallet cards. The {id} route value is the card's
    /// stable UUID - the same value the note subject "Cheatsheet:{id}" carries -
    /// not the backing note's int id.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/cheatsheets")]
    [Produces("application/json")]
    public class CheatsheetsController : Controller
    {
        private readonly ICheatsheetManager _cheatsheetManager;
        private readonly IMapper _mapper;
        private readonly ILogger<CheatsheetsController> _logger;

        public CheatsheetsController(
            ICheatsheetManager cheatsheetManager,
            IMapper mapper,
            ILogger<CheatsheetsController> logger)
        {
            ArgumentNullException.ThrowIfNull(cheatsheetManager);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(logger);

            _cheatsheetManager = cheatsheetManager;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a paginated list of cheatsheet cards.
        /// </summary>
        /// <param name="walletGroup">Optional wallet group filter (case-insensitive).</param>
        /// <param name="tag">Optional tag filter (case-insensitive).</param>
        /// <param name="resourceCollectionParameters">Pagination parameters.</param>
        [HttpGet(Name = "GetCheatsheets")]
        [TypeFilter(typeof(CheatsheetsResultFilter))]
        [ProducesResponseType(typeof(PageList<ApiCheatsheet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> Get(
            [FromQuery] string walletGroup,
            [FromQuery] string tag,
            [FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var result = await _cheatsheetManager.GetCardsAsync(
                walletGroup, tag, resourceCollectionParameters);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to get cheatsheets: {Error}", result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Retrieves a single cheatsheet card by its card id.
        /// </summary>
        [HttpGet("{id}", Name = "GetCheatsheetById")]
        [TypeFilter(typeof(CheatsheetResultFilter))]
        [ProducesResponseType(typeof(ApiCheatsheet), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(string id)
        {
            var result = await _cheatsheetManager.GetCardByIdAsync(id);
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
        /// Creates a new cheatsheet card.
        /// </summary>
        [HttpPost(Name = "AddCheatsheet")]
        [TypeFilter(typeof(CheatsheetResultFilter))]
        [ProducesResponseType(typeof(ApiCheatsheet), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Post(ApiCheatsheetForCreate apiCard)
        {
            if (apiCard == null)
            {
                return BadRequest(new ApiBadRequestResponse("Cheatsheet data is required"));
            }

            var card = _mapper.Map<ApiCheatsheetForCreate, CheatsheetCard>(apiCard);
            var result = await _cheatsheetManager.CreateAsync(card);

            if (!result.Success)
            {
                if (result.ErrorType == Hmm.Utility.Misc.ErrorCategory.Conflict)
                {
                    return Conflict(new ApiBadRequestResponse(result.ErrorMessage));
                }

                _logger.LogWarning("Failed to create cheatsheet: {Error}", result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return CreatedAtRoute("GetCheatsheetById", new { id = result.Value.Id }, result.Value);
        }

        /// <summary>
        /// Replaces a cheatsheet card. The card id comes from the route.
        /// </summary>
        [HttpPut("{id}", Name = "UpdateCheatsheet")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Put(string id, ApiCheatsheetForUpdate apiCard)
        {
            if (apiCard == null)
            {
                return BadRequest(new ApiBadRequestResponse("Cheatsheet data is required"));
            }

            var getResult = await _cheatsheetManager.GetCardByIdAsync(id);
            if (!getResult.Success)
            {
                if (getResult.IsNotFound)
                {
                    return NotFound();
                }

                return BadRequest(new ApiBadRequestResponse(getResult.ErrorMessage));
            }

            // Map onto the stored card so Id / NoteId / AuthorId survive: the
            // mapping profile ignores them precisely so a request body cannot
            // re-identify a card.
            var card = getResult.Value;
            _mapper.Map(apiCard, card);

            var updateResult = await _cheatsheetManager.UpdateAsync(card);
            if (!updateResult.Success)
            {
                if (updateResult.IsNotFound)
                {
                    return NotFound();
                }

                return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
            }

            return NoContent();
        }

        /// <summary>
        /// Deletes a cheatsheet card (soft-deletes the backing note).
        /// </summary>
        [HttpDelete("{id}", Name = "DeleteCheatsheet")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _cheatsheetManager.DeleteAsync(id);
            if (!result.Success)
            {
                if (result.IsNotFound)
                {
                    return NotFound();
                }

                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return NoContent();
        }
    }
}
