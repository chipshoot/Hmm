using AutoMapper;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Filters;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Controllers
{
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/notecatalogs")]
    public class NoteCatalogController : Controller
    {
        #region private fields

        private readonly INoteCatalogManager _catalogManager;
        private readonly IMapper _mapper;
        private readonly IEntityLookup _lookupRepo;

        #endregion private fields

        #region constructor

        public NoteCatalogController(INoteCatalogManager catalogManager, IMapper mapper, IEntityLookup lookupRepo)
        {
            Guard.Against<ArgumentNullException>(catalogManager == null, nameof(catalogManager));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));
            Guard.Against<ArgumentNullException>(lookupRepo == null, nameof(lookupRepo));

            _catalogManager = catalogManager;
            _mapper = mapper;
            _lookupRepo = lookupRepo;
        }

        #endregion constructor

        [HttpGet(Name="GetNoteCatalogs")]
        [NoteCatalogsResultFilter]
        [PaginationFilter]
        public async Task<IActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var noteCatalogs = await _catalogManager.GetEntitiesAsync(null, resourceCollectionParameters);
            if (!noteCatalogs.Any())
            {
                return NotFound();
            }

            return Ok(noteCatalogs);
        }

        [HttpGet("{id:int}", Name = "GetNoteCatalogById")]
        [NoteCatalogResultFilter]
        public async Task<IActionResult> Get(int id)
        {
            var catalog = await _catalogManager.GetEntityByIdAsync(id);
            if (catalog == null)
            {
                return NotFound($"The note catalog: {id} not found.");
            }

            return Ok(catalog);
        }

        // POST api/catalogs
        [HttpPost(Name = "AddNoteCatalog")]
        [NoteCatalogResultFilter]
        public async Task<IActionResult> Post([FromBody] ApiNoteCatalogForCreate catalog)
        {
            try
            {
                var system = await _lookupRepo.GetEntityAsync<Subsystem>(catalog.SubsystemId);
                if (system == null)
                {
                    return BadRequest($"Cannot find note system: {catalog.SubsystemId}.");
                }

                var render = await _lookupRepo.GetEntityAsync<NoteRender>(catalog.RenderId);
                if (render == null)
                {
                    return BadRequest($"Cannot find note render: {catalog.RenderId}.");
                }

                var noteCatalog = _mapper.Map<ApiNoteCatalogForCreate, NoteCatalog>(catalog);
                noteCatalog.Subsystem = system;
                noteCatalog.Render = render;
                var newCatalog = await _catalogManager.CreateAsync(noteCatalog);

                if (newCatalog == null)
                {
                    return BadRequest();
                }

                return Created("", newCatalog);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT api/catalogs/5
        [HttpPut("{id:int}", Name = "UpdateNoteCatalog")]
        public async Task<IActionResult> Put(int id, [FromBody] ApiNoteCatalogForUpdate catalog)
        {
            if (catalog == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("note catalog information is null or invalid id found"));
            }

            try
            {
                var curCatalog = await _catalogManager.GetEntityByIdAsync(id);
                if (curCatalog == null)
                {
                    return BadRequest($"Note catalog {id} cannot be found.");
                }

                curCatalog = _mapper.Map(catalog, curCatalog);
                var newCatalog = await _catalogManager.UpdateAsync(curCatalog);
                if (newCatalog == null)
                {
                    return BadRequest(_catalogManager.ProcessResult.MessageList);
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PATCH api/catalogs/5
        [HttpPatch("{id:int}", Name = "PatchNoteCatalog")]
        public async Task<IActionResult> Patch(int id, [FromBody] JsonPatchDocument<ApiNoteCatalogForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var curCatalog = await _catalogManager.GetEntityByIdAsync(id);
                if (curCatalog == null)
                {
                    return NotFound();
                }

                var catalog2Update = _mapper.Map<ApiNoteCatalogForUpdate>(curCatalog);
                patchDoc.ApplyTo(catalog2Update);
                _mapper.Map(catalog2Update, curCatalog);

                var newCatalog = await _catalogManager.UpdateAsync(curCatalog);
                if (newCatalog == null)
                {
                    return BadRequest(_catalogManager.ProcessResult.MessageList);
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // DELETE api/renders/5
        [HttpDelete("{id:int}")]
        public ActionResult Delete(int id)
        {
            return NoContent();
        }
    }
}