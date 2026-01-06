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
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ContactController> _logger;

        #endregion private fields

        #region constructor

        public ContactController(IContactManager contactManager, IMapper mapper, ILogger<ContactController> logger)
        {
            ArgumentNullException.ThrowIfNull(contactManager);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(logger);

            _contactManager = contactManager;
            _mapper = mapper;
            _logger = logger;
        }

        #endregion constructor

        [HttpGet(Name = "GetContacts")]
        [ContactsResultFilter]
        [CollectionResultFilter]
        public async Task<IActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var contactsResult = await _contactManager.GetContactsAsync(null, resourceCollectionParameters);
            if (!contactsResult.Success)
            {
                _logger.LogError("Failed to retrieve contacts. Error: {ErrorMessage}. TraceId: {TraceId}", contactsResult.ErrorMessage, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving contacts.");
            }

            if (contactsResult.Value == null || !contactsResult.Value.Any())
            {
                return NotFound();
            }

            return Ok(contactsResult.Value);
        }

        [HttpGet("{id:int}", Name = "GetContactById")]
        [ContactResultFilter]
        public async Task<IActionResult> Get(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid contact id");
            }

            var contactResult = await _contactManager.GetContactByIdAsync(id);
            if (!contactResult.Success)
            {
                if (contactResult.IsNotFound)
                {
                    return NotFound($"The contact : {id} not found.");
                }
                _logger.LogError("Failed to retrieve contact with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, contactResult.ErrorMessage, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the contact.");
            }

            return Ok(contactResult.Value);
        }

        // POST api/contacts
        [HttpPost(Name = "AddContact")]
        [ContactResultFilter]
        public async Task<IActionResult> Post(ApiContactForCreate contact)
        {
            try
            {
                var contactObj = _mapper.Map<ApiContactForCreate, Contact>(contact);
                contactObj.IsActivated = true;
                var newContactResult = await _contactManager.CreateAsync(contactObj);

                if (!newContactResult.Success)
                {
                    if (newContactResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(new ApiBadRequestResponse(newContactResult.ErrorMessage));
                    }
                    _logger.LogError("Failed to create contact. Error: {ErrorMessage}. TraceId: {TraceId}", newContactResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the contact.");
                }

                return Created("", newContactResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating contact. TraceId: {TraceId}", HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while creating the contact.");
            }
        }

        // PUT api/contacts/{id}
        [HttpPut("{id:int}", Name = "UpdateContact")]
        public async Task<IActionResult> Put(int id, ApiContactForUpdate contact)
        {
            if (contact == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Contact information is null or invalid id found"));
            }

            try
            {
                var currentContact = _mapper.Map<Contact>(contact);
                currentContact.Id = id;
                var updateResult = await _contactManager.UpdateAsync(currentContact);

                if (!updateResult.Success)
                {
                    if (updateResult.IsNotFound)
                    {
                        return NotFound($"Contact with id {id} not found");
                    }
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
                    }
                    _logger.LogError("Failed to update contact with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the contact.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while updating contact with id {Id}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while updating the contact.");
            }
        }

        // PATCH api/contacts/{id}
        [HttpPatch("{id:int}", Name = "PatchContact")]
        public async Task<IActionResult> Patch(int id, JsonPatchDocument<ApiContactForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var curContactResult = await _contactManager.GetContactByIdAsync(id);
                if (!curContactResult.Success)
                {
                    if (curContactResult.IsNotFound)
                    {
                        return NotFound($"Contact with id {id} not found");
                    }
                    _logger.LogError("Failed to retrieve contact with id {Id} for patching. Error: {ErrorMessage}. TraceId: {TraceId}", id, curContactResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the contact for update.");
                }

                var contact2Update = _mapper.Map<ApiContactForUpdate>(curContactResult.Value);
                patchDoc.ApplyTo(contact2Update);
                _mapper.Map(contact2Update, curContactResult.Value);

                var updateResult = await _contactManager.UpdateAsync(curContactResult.Value);
                if (!updateResult.Success)
                {
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
                    }
                    _logger.LogError("Failed to patch contact with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while patching the contact.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while patching contact with id {Id}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while patching the contact.");
            }
        }

        // DELETE api/contacts/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var deleteResult = await _contactManager.DeActivateAsync(id);

                if (!deleteResult.Success)
                {
                    if (deleteResult.IsNotFound)
                    {
                        return NotFound($"Contact with id {id} not found");
                    }
                    _logger.LogError("Failed to deactivate contact with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, deleteResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deactivating the contact.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while deactivating contact with id {Id}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while deactivating the contact.");
            }
        }
    }
}