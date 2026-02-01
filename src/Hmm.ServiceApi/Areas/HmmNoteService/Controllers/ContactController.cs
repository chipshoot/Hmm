using AutoMapper;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity;
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
using System.Net.Http;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Controllers
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/api/v{version:apiVersion}/contacts")]
    [Produces("application/json")]
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
        [TypeFilter(typeof(ContactsResultFilter))]
        [TypeFilter(typeof(CollectionResultFilter))]
        [ProducesResponseType(typeof(ApiEntityCollection<ApiContact>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var contactsResult = await _contactManager.GetContactsAsync(null, resourceCollectionParameters);
            if (!contactsResult.Success)
            {
                _logger.LogError("Failed to retrieve contacts. Error: {ErrorMessage}. TraceId: {TraceId}", contactsResult.ErrorMessage, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An error occurred while retrieving contacts.", HttpContext));
            }

            // Return 200 OK with empty array when no results (REST best practice)
            return Ok(contactsResult.Value ?? new PageList<Contact>());
        }

        [HttpGet("{id:int}", Name = "GetContactById")]
        [TypeFilter(typeof(ContactResultFilter))]
        [ProducesResponseType(typeof(ApiContact), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(int id)
        {
            if (id <= 0)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest("Invalid contact id", HttpContext));
            }

            var contactResult = await _contactManager.GetContactByIdAsync(id);
            if (!contactResult.Success)
            {
                if (contactResult.IsNotFound)
                {
                    return NotFound(ProblemDetailsHelper.NotFound($"The contact : {id} not found.", HttpContext));
                }
                _logger.LogError("Failed to retrieve contact with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, contactResult.ErrorMessage, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An error occurred while retrieving the contact.", HttpContext));
            }

            return Ok(contactResult.Value);
        }

        // POST api/contacts
        [HttpPost(Name = "AddContact")]
        [TypeFilter(typeof(ContactResultFilter))]
        [ProducesResponseType(typeof(ApiContact), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
                        return BadRequest(ProblemDetailsHelper.BadRequest(newContactResult.ErrorMessage, HttpContext));
                    }
                    _logger.LogError("Failed to create contact. Error: {ErrorMessage}. TraceId: {TraceId}", newContactResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while creating the contact.", HttpContext));
                }

                return CreatedAtRoute("GetContactById", new { id = newContactResult.Value.Id, version = "1.0" }, newContactResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating contact. TraceId: {TraceId}", HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while creating the contact.", HttpContext));
            }
        }

        // PUT api/contacts/{id}
        [HttpPut("{id:int}", Name = "UpdateContact")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Put(int id, ApiContactForUpdate contact)
        {
            if (contact == null || id <= 0)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest("Contact information is null or invalid id found", HttpContext));
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
                        return NotFound(ProblemDetailsHelper.NotFound($"Contact with id {id} not found", HttpContext));
                    }
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(ProblemDetailsHelper.BadRequest(updateResult.ErrorMessage, HttpContext));
                    }
                    _logger.LogError("Failed to update contact with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while updating the contact.", HttpContext));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while updating contact with id {Id}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while updating the contact.", HttpContext));
            }
        }

        // PATCH api/contacts/{id}
        [HttpPatch("{id:int}", Name = "PatchContact")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Patch(int id, JsonPatchDocument<ApiContactForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest("Patch information is null or invalid id found", HttpContext));
            }

            try
            {
                var curContactResult = await _contactManager.GetContactByIdAsync(id);
                if (!curContactResult.Success)
                {
                    if (curContactResult.IsNotFound)
                    {
                        return NotFound(ProblemDetailsHelper.NotFound($"Contact with id {id} not found", HttpContext));
                    }
                    _logger.LogError("Failed to retrieve contact with id {Id} for patching. Error: {ErrorMessage}. TraceId: {TraceId}", id, curContactResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while retrieving the contact for update.", HttpContext));
                }

                var contact2Update = _mapper.Map<ApiContactForUpdate>(curContactResult.Value);
                patchDoc.ApplyTo(contact2Update, ModelState);
                if (!TryValidateModel(contact2Update))
                {
                    return BadRequest(ProblemDetailsHelper.ValidationError(ModelState, HttpContext));
                }
                _mapper.Map(contact2Update, curContactResult.Value);

                var updateResult = await _contactManager.UpdateAsync(curContactResult.Value);
                if (!updateResult.Success)
                {
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(ProblemDetailsHelper.BadRequest(updateResult.ErrorMessage, HttpContext));
                    }
                    _logger.LogError("Failed to patch contact with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while patching the contact.", HttpContext));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while patching contact with id {Id}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while patching the contact.", HttpContext));
            }
        }

        // DELETE api/contacts/{id}
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var deleteResult = await _contactManager.DeActivateAsync(id);

                if (!deleteResult.Success)
                {
                    if (deleteResult.IsNotFound)
                    {
                        return NotFound(ProblemDetailsHelper.NotFound($"Contact with id {id} not found", HttpContext));
                    }
                    _logger.LogError("Failed to deactivate contact with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, deleteResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        ProblemDetailsHelper.InternalServerError("An error occurred while deactivating the contact.", HttpContext));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while deactivating contact with id {Id}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError("An unexpected error occurred while deactivating the contact.", HttpContext));
            }
        }
    }
}
