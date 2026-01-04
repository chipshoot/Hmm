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

        #endregion private fields

        #region constructor

        public AuthorController(IAuthorManager authorManager, IMapper mapper)
        {
            ArgumentNullException.ThrowIfNull(authorManager);
            ArgumentNullException.ThrowIfNull(mapper);
            //ArgumentNullException.ThrowIfNull(httpClientFactory);

            _authorManager = authorManager;
            _mapper = mapper;
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
                return StatusCode(StatusCodes.Status500InternalServerError, authorsResult.ErrorMessage);
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
                return StatusCode(StatusCodes.Status500InternalServerError, authorResult.ErrorMessage);
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
                    return StatusCode(StatusCodes.Status500InternalServerError, newAuthorResult.ErrorMessage);
                }

                return Created("", newAuthorResult.Value);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
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
                    return StatusCode(StatusCodes.Status500InternalServerError, updateResult.ErrorMessage);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
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
                    return StatusCode(StatusCodes.Status500InternalServerError, curUsrResult.ErrorMessage);
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
                    return StatusCode(StatusCodes.Status500InternalServerError, updateResult.ErrorMessage);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
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
                    return StatusCode(StatusCodes.Status500InternalServerError, deleteResult.ErrorMessage);
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