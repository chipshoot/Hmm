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
    [Route("/v{version:apiVersion}/authors")]
    public class AuthorController : Controller
    {
        #region private fields

        private readonly IAuthorManager _authorManager;
        private readonly IMapper _mapper;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AuthorController> _logger;

        #endregion private fields

        #region constructor

        public AuthorController(IAuthorManager authorManager, IMapper mapper, ILogger<AuthorController> logger)
        {
            ArgumentNullException.ThrowIfNull(authorManager);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(logger);
            //ArgumentNullException.ThrowIfNull(httpClientFactory);

            _authorManager = authorManager;
            _mapper = mapper;
            _logger = logger;
            //_httpClientFactory = httpClientFactory;
        }

        #endregion constructor

        [HttpGet(Name = "GetAuthors")]
        [AuthorsResultFilter]
        [CollectionResultFilter]
        public async Task<IActionResult> Get([FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var authorsResult = await _authorManager.GetEntitiesAsync(null, resourceCollectionParameters);
            if (!authorsResult.Success)
            {
                _logger.LogError("Failed to retrieve authors. Error: {ErrorMessage}. TraceId: {TraceId}", authorsResult.ErrorMessage, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving authors.");
            }

            if (authorsResult.Value == null || !authorsResult.Value.Any())
            {
                return NotFound();
            }

            return Ok(authorsResult.Value);
        }

        [HttpGet("{id:int}", Name = "GetAuthorById")]
        [AuthorResultFilter]
        public async Task<IActionResult> Get(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid author id");
            }

            var authorResult = await _authorManager.GetAuthorByIdAsync(id);
            if (!authorResult.Success)
            {
                if (authorResult.IsNotFound)
                {
                    return NotFound($"The author {id} cannot be found.");
                }
                _logger.LogError("Failed to retrieve author with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, authorResult.ErrorMessage, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the author.");
            }

            return Ok(authorResult.Value);
        }

        //        //[HttpGet("{subject}", Name = "GetAuthorBySubject")]
        //        //public async Task<IActionResult> Get(string subject)
        //        //{
        //        //    var id = new Guid(subject);
        //        //    var author = _authorManager.GetEntities().FirstOrDefault(u => u.Id == id);
        //        //    if (author == null)
        //        //    {
        //        //        // subject must come from token
        //        //        var subjectFromToken = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        //        //        if (subjectFromToken == null)
        //        //        {
        //        //            var error = "null user subject found";
        //        //            Log.Logger.Error(error);
        //        //            return BadRequest(new ApiBadRequestResponse(error));
        //        //        }

        //        //        var httpClient = _httpClientFactory.CreateClient(HmmServiceApiConstants.HttpClient.Idp);
        //        //        var userName = await IdpUserProfileProvider.GetUserClaimAsync("name", HttpContext, httpClient);
        //        //        if (string.IsNullOrEmpty(userName))
        //        //        {
        //        //            Log.Logger.Error($"Cannot get username for subject {subjectFromToken}");
        //        //            return StatusCode(StatusCodes.Status500InternalServerError);
        //        //        }

        //        //        var authorToCreate = new Author
        //        //        {
        //        //            Id = new Guid(subjectFromToken),
        //        //            AccountName = userName,
        //        //            Role = AuthorRoleType.Guest,
        //        //            IsActivated = true
        //        //        };

        //        //        author = _authorManager.Create(authorToCreate);
        //        //    }

        //        //    var ret = _mapper.Map<Author, ApiAuthor>(author);
        //        //    return Ok(ret);
        //        //}

        // POST api/authors
        [HttpPost(Name = "AddAuthor")]
        [AuthorResultFilter]
        public async Task<IActionResult> Post(ApiAuthorForCreate author)
        {
            try
            {
                var usr = _mapper.Map<ApiAuthorForCreate, Author>(author);
                usr.IsActivated = true;
                var newAuthorResult = await _authorManager.CreateAsync(usr);

                if (!newAuthorResult.Success)
                {
                    if (newAuthorResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(new ApiBadRequestResponse(newAuthorResult.ErrorMessage));
                    }
                    _logger.LogError("Failed to create author. Error: {ErrorMessage}. TraceId: {TraceId}", newAuthorResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the author.");
                }

                return Created("", newAuthorResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating author. TraceId: {TraceId}", HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while creating the author.");
            }
        }

        // PUT api/authors/5
        [HttpPut("{id:int}", Name = "UpdateAuthor")]
        public async Task<IActionResult> Put(int id, ApiAuthorForUpdate author)
        {
            if (author == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Author information is null or invalid id found"));
            }

            try
            {
                var currentAuthor = _mapper.Map<Author>(author);
                currentAuthor.Id = id;
                var updateResult = await _authorManager.UpdateAsync(currentAuthor);

                if (!updateResult.Success)
                {
                    if (updateResult.IsNotFound)
                    {
                        return NotFound($"Author with id {id} not found");
                    }
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
                    }
                    _logger.LogError("Failed to update author with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the author.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while updating author with id {Id}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while updating the author.");
            }
        }

        // PATCH api/authors/5
        [HttpPatch("{id:int}", Name = "PatchAuthor")]
        public async Task<IActionResult> Patch(int id, JsonPatchDocument<ApiAuthorForUpdate> patchDoc)
        {
            if (patchDoc == null || id <= 0)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var curUsrResult = await _authorManager.GetAuthorByIdAsync(id);
                if (!curUsrResult.Success)
                {
                    if (curUsrResult.IsNotFound)
                    {
                        return NotFound($"Author with id {id} not found");
                    }
                    _logger.LogError("Failed to retrieve author with id {Id} for patching. Error: {ErrorMessage}. TraceId: {TraceId}", id, curUsrResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the author for update.");
                }

                var author2Update = _mapper.Map<ApiAuthorForUpdate>(curUsrResult.Value);
                patchDoc.ApplyTo(author2Update);
                _mapper.Map(author2Update, curUsrResult.Value);

                var updateResult = await _authorManager.UpdateAsync(curUsrResult.Value);
                if (!updateResult.Success)
                {
                    if (updateResult.ErrorType == ErrorCategory.ValidationError)
                    {
                        return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
                    }
                    _logger.LogError("Failed to patch author with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, updateResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while patching the author.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while patching author with id {Id}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while patching the author.");
            }
        }

        // DELETE api/authors/5
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var deleteResult = await _authorManager.DeActivateAsync(id);

                if (!deleteResult.Success)
                {
                    if (deleteResult.IsNotFound)
                    {
                        return NotFound($"Author with id {id} not found");
                    }
                    _logger.LogError("Failed to deactivate author with id {Id}. Error: {ErrorMessage}. TraceId: {TraceId}", id, deleteResult.ErrorMessage, HttpContext.TraceIdentifier);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deactivating the author.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while deactivating author with id {Id}. TraceId: {TraceId}", id, HttpContext.TraceIdentifier);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while deactivating the author.");
            }
        }
    }
}