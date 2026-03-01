using Hmm.ServiceApi.DtoEntity.Currency;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Hmm.ServiceApi.Areas.UtilityService.Controllers
{
    [Authorize]
    [ApiController]
    [EnableCors("AllowCors")]
    [ApiVersion("1.0")]
    [Route("/api/v{version:apiVersion}/currency")]
    [Produces("application/json")]
    public class CurrencyController : Controller
    {
        /// <summary>
        /// Returns the exchange rate between two currencies.
        /// Placeholder implementation: always returns 1.0.
        /// </summary>
        /// <param name="from">Source currency code (e.g. CAD, USD, CNY).</param>
        /// <param name="to">Target currency code (e.g. CAD, USD, CNY).</param>
        [HttpGet("exchange-rate", Name = "GetExchangeRate")]
        [ProducesResponseType(typeof(ExchangeRateResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public ActionResult<ExchangeRateResponse> GetExchangeRate(
            [FromQuery] string from,
            [FromQuery] string to)
        {
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid request",
                    Detail = "Both 'from' and 'to' currency codes are required."
                });
            }

            return Ok(new ExchangeRateResponse
            {
                From = from.ToUpperInvariant(),
                To = to.ToUpperInvariant(),
                Rate = 1.0m,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
