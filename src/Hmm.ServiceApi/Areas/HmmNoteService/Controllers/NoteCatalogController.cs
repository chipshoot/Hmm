using AutoMapper;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Controllers
{
    [Authorize]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/notecatalogs")]
    public class NoteCatalogController : Controller
    {
        #region private fields

        private readonly INoteCatalogManager _catalogManager;
        private readonly IMapper _mapper;
        private readonly ILogger<NoteCatalogController> _logger;

        #endregion private fields

        #region constructor

        public NoteCatalogController(INoteCatalogManager catalogManager, IMapper mapper, ILogger<NoteCatalogController> logger)
        {
            ArgumentNullException.ThrowIfNull(catalogManager);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(logger);

            _catalogManager = catalogManager;
            _mapper = mapper;
            _logger = logger;
        }

        #endregion constructor

        [HttpGet(Name = "GetNoteCatalogs")]
        [TypeFilter(typeof(NoteCatalogsResultFilter))]
        [TypeFilter(typeof(CollectionResultFilter))]
        public async Task<IActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var noteCatalogsResult = await _catalogManager.GetEntitiesAsync(null, resourceCollectionParameters);
            if (!noteCatalogsResult.Success)
            {
                _logger.LogError("Failed to retrieve note catalogs. Error: {ErrorMessage}. TraceId: {TraceId}", noteCatalogsResult.ErrorMessage, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An error occurred while retrieving note catalogs.", HttpContext));
            }

            // Return 200 OK with empty array when no results (REST best practice)
            return Ok(noteCatalogsResult.Value ?? new PageList<NoteCatalog>());
        }

        [HttpGet("{id:int}", Name = "GetNoteCatalogById")]
        [TypeFilter(typeof(NoteCatalogResultFilter))]
        public async Task<IActionResult> Get(int id)
        {
            var catalogResult = await _catalogManager.GetEntityByIdAsync(id);
            if (!catalogResult.Success)
            {
                if (catalogResult.IsNotFound)
                {
                    return NotFound(ProblemDetailsHelper.NotFound($"The note catalog: {id} not found.", HttpContext));
                }
                _logger.LogError("Failed to retrieve note catalog with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, catalogResult.ErrorMessage, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An error occurred while retrieving the note catalog.", HttpContext));
            }

            return Ok(catalogResult.Value);
        }

        // POST api/notecatalogs
        [HttpPost(Name = "AddNoteCatalog")]
        [TypeFilter(typeof(NoteCatalogResultFilter))]
        public async Task<IActionResult> Post([FromBody] ApiNoteCatalogForCreate catalog)
        {
            try
            {
                var noteCatalog = _mapper.Map<ApiNoteCatalogForCreate, NoteCatalog>(catalog);
                var newCatalogResult = await _catalogManager.CreateAsync(noteCatalog);

                if (!newCatalogResult.Success)
                {
                    if (newCatalogResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(ProblemDetailsHelper.BadRequest(newCatalogResult.ErrorMessage, HttpContext));
                    }
                    _logger.LogError("Failed to create note catalog. Error: {ErrorMessage}. TraceId: {TraceId}", newCatalogResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while creating the note catalog.", HttpContext));
                }

                return CreatedAtRoute("GetNoteCatalogById", new { id = newCatalogResult.Value.Id, version = "1.0" }, newCatalogResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating note catalog. TraceId: {TraceId}", HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while creating the note catalog.", HttpContext));
            }
        }

        // PUT api/notecatalogs/{id}
        [HttpPut("{id:int}", Name = "UpdateNoteCatalog")]
        public async Task<IActionResult> Put(int id, [FromBody] ApiNoteCatalogForUpdate catalog)
        {
            if (catalog == null || id <= 0)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest("Note catalog information is null or invalid id found", HttpContext));
            }

            try
            {
                var curCatalogResult = await _catalogManager.GetEntityByIdAsync(id);
                if (!curCatalogResult.Success)
                {
                    if (curCatalogResult.IsNotFound)
                    {
                        return NotFound(ProblemDetailsHelper.NotFound($"Note catalog {id} cannot be found.", HttpContext));
                    }
                    _logger.LogError("Failed to retrieve note catalog with id {Id} for updating. Error: {ErrorMessage}. TraceId: {TraceId}", id, curCatalogResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while retrieving the note catalog for update.", HttpContext));
                }

                var curCatalog = _mapper.Map(catalog, curCatalogResult.Value);
                var updateResult = await _catalogManager.UpdateAsync(curCatalog);

                if (!updateResult.Success)
                {
                    if (updateResult.IsNotFound)
                    {
                        return NotFound(ProblemDetailsHelper.NotFound($"Note catalog with id {id} not found", HttpContext));
                    }
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(ProblemDetailsHelper.BadRequest(updateResult.ErrorMessage, HttpContext));
                    }
                    _logger.LogError("Failed to update note catalog with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while updating the note catalog.", HttpContext));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while updating note catalog with id {Id}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while updating the note catalog.", HttpContext));
            }
        }

        // PATCH api/notecatalogs/{id}
        [HttpPatch("{id:int}", Name = "PatchNoteCatalog")]
        public async Task<IActionResult> Patch(int id, [FromBody] JsonPatchDocument<ApiNoteCatalogForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest("Patch information is null or invalid id found", HttpContext));
            }

            try
            {
                var curCatalogResult = await _catalogManager.GetEntityByIdAsync(id);
                if (!curCatalogResult.Success)
                {
                    if (curCatalogResult.IsNotFound)
                    {
                        return NotFound(ProblemDetailsHelper.NotFound($"Note catalog with id {id} not found", HttpContext));
                    }
                    _logger.LogError("Failed to retrieve note catalog with id {Id} for patching. Error: {ErrorMessage}. TraceId: {TraceId}", id, curCatalogResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while retrieving the note catalog for update.", HttpContext));
                }

                var catalog2Update = _mapper.Map<ApiNoteCatalogForUpdate>(curCatalogResult.Value);
                patchDoc.ApplyTo(catalog2Update, ModelState);
                if (!TryValidateModel(catalog2Update))
                {
                    return BadRequest(ProblemDetailsHelper.ValidationError(ModelState, HttpContext));
                }
                _mapper.Map(catalog2Update, curCatalogResult.Value);

                var updateResult = await _catalogManager.UpdateAsync(curCatalogResult.Value);
                if (!updateResult.Success)
                {
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(ProblemDetailsHelper.BadRequest(updateResult.ErrorMessage, HttpContext));
                    }
                    _logger.LogError("Failed to patch note catalog with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while patching the note catalog.", HttpContext));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while patching note catalog with id {Id}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while patching the note catalog.", HttpContext));
            }
        }
    }
}
