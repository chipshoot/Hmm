using AutoMapper;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Controllers
{
    [Route("api/notecatalogs")]
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

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var catalog = _catalogManager.GetEntities().FirstOrDefault(c => c.Id == id);
            var ret = _mapper.Map<NoteCatalog, ApiNoteCatalog>(catalog);
            return Ok(ret);
        }

        // POST api/catalogs
        [HttpPost]
        public IActionResult Post(int renderId, [FromBody] ApiNoteCatalogForCreate catalog)
        {
            try
            {
                var render = _lookupRepo.GetEntity<NoteRender>(renderId);
                if (render == null)
                {
                    return BadRequest("Cannot find note render");
                }

                var noteCatalog = _mapper.Map<ApiNoteCatalogForCreate, NoteCatalog>(catalog);
                noteCatalog.Render = render;
                var newCatalog = _catalogManager.Create(noteCatalog);

                if (newCatalog == null)
                {
                    return BadRequest();
                }

                var apiNewCatalog = _mapper.Map<NoteCatalog, ApiNoteCatalog>(newCatalog);

                return Ok(apiNewCatalog);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT api/catalogs/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] ApiNoteCatalogForUpdate catalog)
        {
            if (catalog == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("note catalog information is null or invalid id found"));
            }

            try
            {
                var curCatalog = _catalogManager.GetEntities().FirstOrDefault(c => c.Id == id);
                if (curCatalog == null)
                {
                    return NotFound();
                }

                curCatalog = _mapper.Map(catalog, curCatalog);
                var apiNewCatalog = _catalogManager.Update(curCatalog);
                if (apiNewCatalog == null)
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
        [HttpPatch("{id}")]
        public IActionResult Patch(int id, [FromBody] JsonPatchDocument<ApiNoteCatalogForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var curCatalog = _catalogManager.GetEntities().FirstOrDefault(c => c.Id == id);
                if (curCatalog == null)
                {
                    return NotFound();
                }

                var catalog2Update = _mapper.Map<ApiNoteCatalogForUpdate>(curCatalog);
                patchDoc.ApplyTo(catalog2Update);
                _mapper.Map(catalog2Update, curCatalog);

                var newCatalog = _catalogManager.Update(curCatalog);
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
        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            return NoContent();
        }
    }
}