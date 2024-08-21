using AutoMapper;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
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
using System.Net.Http;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/contacts")]
    public class ContactController : Controller
    {
        #region private fields

        private readonly IContactManager _contactManager;
        private readonly IMapper _mapper;

        #endregion private fields

        #region constructor

        public ContactController(IContactManager contactManager, IMapper mapper)
        {
            Guard.Against<ArgumentNullException>(contactManager == null, nameof(contactManager));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));

            _contactManager = contactManager;
            _mapper = mapper;
        }

        #endregion constructor

        [HttpGet(Name = "GetContacts")]
        [ContactResultFilter]
        [CollectionResultFilter]
        public async Task<IActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var contacts = await _contactManager.GetContactsAsync(null, resourceCollectionParameters);
            if (!contacts.Any())
            {
                return NotFound();
            }

            return Ok(contacts);
        }

        [HttpGet("{id:int}", Name = "GetContactById")]
        [AuthorResultFilter]
        public async Task<IActionResult> Get(int id)
        {
            if (id <= 0)
            {
                BadRequest();
            }

            var contact = await _contactManager.GetContactByIdAsync(id);
            if (contact == null)
            {
                return NotFound($"The contact : {id} not found.");
            }

            return Ok(contact);
        }

        // POST api/authors
        [HttpPost(Name = "AddContact")]
        [AuthorResultFilter]
        public async Task<IActionResult> Post(ApiContactForCreate contact)
        {
            try
            {
                var contactObj = _mapper.Map<ApiContactForCreate, Contact>(contact);
                contactObj.IsActivated = true;
                var newContact = await _contactManager.CreateAsync(contactObj);

                if (newContact == null)
                {
                    return BadRequest(new ApiBadRequestResponse("null contact found"));
                }

                return Created("", newContact);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT api/authors/5
        [HttpPut("{id:int}", Name = "UpdateContact")]
        public async Task<IActionResult> Put(int id, ApiContactForUpdate contact)
        {
            if (contact == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Contact information is null or invalid id found"));
            }

            try
            {
                var currentContact = await _contactManager.GetContactByIdAsync(id);
                if (currentContact == null)
                {
                    return BadRequest($"The contact {id} cannot be found.");
                }

                currentContact = _mapper.Map(contact, currentContact);
                var newContact = await _contactManager.UpdateAsync(currentContact);
                if (newContact == null)
                {
                    return BadRequest(_contactManager.ProcessResult.MessageList);
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PATCH api/authors/5
        [HttpPatch("{id:int}", Name = "PatchContact")]
        public async Task<IActionResult> Patch(int id, JsonPatchDocument<ApiContactForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var curContact = await _contactManager.GetContactByIdAsync(id);
                if (curContact == null)
                {
                    return NotFound();
                }

                var contact2Update = _mapper.Map<ApiContactForUpdate>(curContact);
                patchDoc.ApplyTo(contact2Update);
                _mapper.Map(contact2Update, curContact);

                var newContact = await _contactManager.UpdateAsync(curContact);
                if (newContact == null)
                {
                    return BadRequest(_contactManager.ProcessResult.MessageList);
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // DELETE api/authors/5
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var contactExists = await _contactManager.GetContactByIdAsync(id);
            if (contactExists==null)
            {
                return BadRequest($"The contact {id} cannot be found.");
            }

            try
            {
                await _contactManager.DeActivateAsync(id);
                if (_contactManager.ProcessResult.Success)
                {
                    return NoContent();
                }

                throw new Exception($"Deleting contact {id} failed on saving.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}