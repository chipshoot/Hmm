using AutoMapper;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Controllers
{
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/notecatalogs")]
    public class NoteCatalogController : Controller
    {
        #region private fields

        private readonly INoteCatalogManager _catalogManager;
        private readonly IMapper _mapper;

        #endregion private fields

        #region constructor

        public NoteCatalogController(INoteCatalogManager catalogManager, IMapper mapper)
        {
            ArgumentNullException.ThrowIfNull(catalogManager);
            ArgumentNullException.ThrowIfNull(mapper);

            _catalogManager = catalogManager;
            _mapper = mapper;
        }

        #endregion constructor

        [HttpGet(Name = "GetNoteCatalogs")]
        [NoteCatalogsResultFilter]
        [CollectionResultFilter]
        public async Task<IActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var noteCatalogsResult = await _catalogManager.GetEntitiesAsync(null, resourceCollectionParameters);
            if (!noteCatalogsResult.Success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, noteCatalogsResult.ErrorMessage);
            }

            if (noteCatalogsResult.Value == null || !noteCatalogsResult.Value.Any())
            {
                return NotFound();
            }

            return Ok(noteCatalogsResult.Value);
        }

        [HttpGet("{id:int}", Name = "GetNoteCatalogById")]
        [NoteCatalogResultFilter]
        public async Task<IActionResult> Get(int id)
        {
            var catalogResult = await _catalogManager.GetEntityByIdAsync(id);
            if (!catalogResult.Success)
            {
                if (catalogResult.IsNotFound)
                {
                    return NotFound($"The note catalog: {id} not found.");
                }
                return StatusCode(StatusCodes.Status500InternalServerError, catalogResult.ErrorMessage);
            }

            return Ok(catalogResult.Value);
        }

        // POST api/notecatalogs
        [HttpPost(Name = "AddNoteCatalog")]
        [NoteCatalogResultFilter]
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
                        return BadRequest(new ApiBadRequestResponse(newCatalogResult.ErrorMessage));
                    }
                    return StatusCode(StatusCodes.Status500InternalServerError, newCatalogResult.ErrorMessage);
                }

                return Created("", newCatalogResult.Value);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // PUT api/notecatalogs/{id}
        [HttpPut("{id:int}", Name = "UpdateNoteCatalog")]
        public async Task<IActionResult> Put(int id, [FromBody] ApiNoteCatalogForUpdate catalog)
        {
            if (catalog == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Note catalog information is null or invalid id found"));
            }

            try
            {
                var curCatalogResult = await _catalogManager.GetEntityByIdAsync(id);
                if (!curCatalogResult.Success)
                {
                    if (curCatalogResult.IsNotFound)
                    {
                        return NotFound($"Note catalog {id} cannot be found.");
                    }
                    return StatusCode(StatusCodes.Status500InternalServerError, curCatalogResult.ErrorMessage);
                }

                var curCatalog = _mapper.Map(catalog, curCatalogResult.Value);
                var updateResult = await _catalogManager.UpdateAsync(curCatalog);

                if (!updateResult.Success)
                {
                    if (updateResult.IsNotFound)
                    {
                        return NotFound($"Note catalog with id {id} not found");
                    }
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
                    }
                    return StatusCode(StatusCodes.Status500InternalServerError, updateResult.ErrorMessage);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // PATCH api/notecatalogs/{id}
        [HttpPatch("{id:int}", Name = "PatchNoteCatalog")]
        public async Task<IActionResult> Patch(int id, [FromBody] JsonPatchDocument<ApiNoteCatalogForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var curCatalogResult = await _catalogManager.GetEntityByIdAsync(id);
                if (!curCatalogResult.Success)
                {
                    if (curCatalogResult.IsNotFound)
                    {
                        return NotFound($"Note catalog with id {id} not found");
                    }
                    return StatusCode(StatusCodes.Status500InternalServerError, curCatalogResult.ErrorMessage);
                }

                var catalog2Update = _mapper.Map<ApiNoteCatalogForUpdate>(curCatalogResult.Value);
                patchDoc.ApplyTo(catalog2Update);
                _mapper.Map(catalog2Update, curCatalogResult.Value);

                var updateResult = await _catalogManager.UpdateAsync(curCatalogResult.Value);
                if (!updateResult.Success)
                {
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
                    }
                    return StatusCode(StatusCodes.Status500InternalServerError, updateResult.ErrorMessage);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}