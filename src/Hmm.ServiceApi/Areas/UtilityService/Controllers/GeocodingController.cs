using AutoMapper;
using Hmm.ServiceApi.DtoEntity.Utility;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.UtilityService.Controllers
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/api/v{version:apiVersion}/geocoding")]
    [Produces("application/json")]
    public class GeocodingController : Controller
    {
        private readonly IGeocodingService _geocodingService;
        private readonly IMapper _mapper;
        private readonly ILogger<GeocodingController> _logger;

        public GeocodingController(
            IGeocodingService geocodingService,
            IMapper mapper,
            ILogger<GeocodingController> logger)
        {
            ArgumentNullException.ThrowIfNull(geocodingService);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(logger);

            _geocodingService = geocodingService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Gets address information from latitude and longitude coordinates.
        /// </summary>
        /// <param name="latitude">Latitude (-90 to 90).</param>
        /// <param name="longitude">Longitude (-180 to 180).</param>
        /// <returns>Address information for the specified coordinates.</returns>
        [HttpGet("reverse", Name = "ReverseGeocode")]
        [ProducesResponseType(typeof(ApiGeoAddress), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReverseGeocode(
            [FromQuery] double latitude,
            [FromQuery] double longitude)
        {
            var result = await _geocodingService.ReverseGeocodeAsync(latitude, longitude);
            if (!result.Success)
            {
                if (result.IsNotFound)
                {
                    return NotFound(new ApiBadRequestResponse(result.ErrorMessage));
                }

                _logger.LogWarning("Reverse geocoding failed: {Error}", result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            var apiAddress = _mapper.Map<ApiGeoAddress>(result.Value);
            return Ok(apiAddress);
        }
    }
}
