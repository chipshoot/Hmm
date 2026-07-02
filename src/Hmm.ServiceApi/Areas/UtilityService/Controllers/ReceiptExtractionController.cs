using Hmm.ServiceApi.DtoEntity.Utility;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.UtilityService.Controllers
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/receipts")]
    [Produces("application/json")]
    public class ReceiptExtractionController : Controller
    {
        private static readonly HashSet<string> AllowedContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg", "image/png", "image/heic", "image/webp", "application/pdf"
            };

        private const long MaxBytes = 8L * 1024 * 1024;

        private readonly IReceiptExtractionService _service;
        private readonly ILogger<ReceiptExtractionController> _logger;

        public ReceiptExtractionController(
            IReceiptExtractionService service,
            ILogger<ReceiptExtractionController> logger)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(logger);

            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Extract structured data from a receipt image or PDF (multipart upload).
        /// </summary>
        [HttpPost("extract", Name = "ExtractReceipt")]
        [ProducesResponseType(typeof(ApiReceiptDraft), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status415UnsupportedMediaType)]
        public async Task<IActionResult> Extract(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiBadRequestResponse("A receipt file is required."));
            }
            if (!AllowedContentTypes.Contains(file.ContentType))
            {
                return StatusCode(StatusCodes.Status415UnsupportedMediaType,
                    new ApiBadRequestResponse($"Unsupported content type '{file.ContentType}'."));
            }
            if (file.Length > MaxBytes)
            {
                return StatusCode(StatusCodes.Status413PayloadTooLarge,
                    new ApiBadRequestResponse("Receipt exceeds the 8 MB limit."));
            }

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            var result = await _service.ExtractAsync(bytes, file.ContentType);
            if (!result.Success)
            {
                _logger.LogWarning("Receipt extraction failed: {Error}", result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return Ok(ToDto(result.Value));
        }

        private static ApiReceiptDraft ToDto(ReceiptExtractionResult r) => new()
        {
            ShopName = r.ShopName,
            Date = r.Date,
            Odometer = r.Odometer,
            Tax = r.Tax,
            Total = r.Total,
            Currency = r.Currency,
            LineItems = r.LineItems.Select(li => new ApiReceiptLineItem
            {
                Type = li.Type,
                Name = li.Name,
                Quantity = li.Quantity,
                UnitCost = li.UnitCost
            }).ToList()
        };
    }
}
