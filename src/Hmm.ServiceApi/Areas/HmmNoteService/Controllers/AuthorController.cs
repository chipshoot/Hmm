using AutoMapper;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Models;
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
    [Route("api/v{version:apiVersion}/authors")]
    public class AuthorController : Controller
    {
        #region private fields

        private readonly IAuthorManager _authorManager;
        private readonly IMapper _mapper;
        private readonly IHttpClientFactory _httpClientFactory;

        #endregion private fields

        #region constructor

        public AuthorController(IAuthorManager authorManager, IMapper mapper, IHttpClientFactory httpClientFactory)
        {
            Guard.Against<ArgumentNullException>(authorManager == null, nameof(authorManager));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));
            Guard.Against<ArgumentNullException>(httpClientFactory == null, nameof(httpClientFactory));

            _authorManager = authorManager;
            _mapper = mapper;
            _httpClientFactory = httpClientFactory;
        }

        #endregion constructor

        [HttpGet(Name = "GetAuthors")]
        [AuthorsResultFilter]
        public async Task<IActionResult> Get()
        {
            var authors = await _authorManager.GetEntitiesAsync();
            if (!authors.Any())
            {
                return NotFound();
            }

            return Ok(authors);
        }

        [HttpGet("{id:guid}", Name = "GetAuthorById")]
        [AuthorResultFilter]
        public async Task<IActionResult> Get(Guid id)
        {
            if (id == Guid.Empty)
            {
                BadRequest();
            }

            var author = await _authorManager.GetAuthorByIdAsync(id);
            if (author == null)
            {
                return NotFound();
            }

            return Ok(author);
        }

        //[HttpGet("{subject}", Name = "GetAuthorBySubject")]
        //public async Task<IActionResult> Get(string subject)
        //{
        //    var id = new Guid(subject);
        //    var author = _authorManager.GetEntities().FirstOrDefault(u => u.Id == id);
        //    if (author == null)
        //    {
        //        // subject must come from token
        //        var subjectFromToken = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        //        if (subjectFromToken == null)
        //        {
        //            var error = "null user subject found";
        //            Log.Logger.Error(error);
        //            return BadRequest(new ApiBadRequestResponse(error));
        //        }

        //        var httpClient = _httpClientFactory.CreateClient(HmmServiceApiConstants.HttpClient.Idp);
        //        var userName = await IdpUserProfileProvider.GetUserClaimAsync("name", HttpContext, httpClient);
        //        if (string.IsNullOrEmpty(userName))
        //        {
        //            Log.Logger.Error($"Cannot get username for subject {subjectFromToken}");
        //            return StatusCode(StatusCodes.Status500InternalServerError);
        //        }

        //        var authorToCreate = new Author
        //        {
        //            Id = new Guid(subjectFromToken),
        //            AccountName = userName,
        //            Role = AuthorRoleType.Guest,
        //            IsActivated = true
        //        };

        //        author = _authorManager.Create(authorToCreate);
        //    }

        //    var ret = _mapper.Map<Author, ApiAuthor>(author);
        //    return Ok(ret);
        //}

        // POST api/authors
        [HttpPost(Name = "AddAuthor")]
        [AuthorResultFilter]
        public async Task<IActionResult> CreateAuthor(ApiAuthorForCreate author)
        {
            try
            {
                //var subject = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                //if (string.IsNullOrEmpty(subject))
                //{
                //    return BadRequest();
                //}

                //if (_authorManager.AuthorExists(subject))
                //{
                //    return BadRequest();
                //}

                var usr = _mapper.Map<ApiAuthorForCreate, Author>(author);
                usr.IsActivated = true;
                var newAuthor = await _authorManager.CreateAsync(usr);

                if (newAuthor == null)
                {
                    return BadRequest(new ApiBadRequestResponse("null author found"));
                }

                return Created("", newAuthor);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT api/authors/5
        [HttpPut("{id:guid}", Name = "UpdateAuthor")]
        public async Task<IActionResult> Put(Guid id, ApiAuthorForUpdate author)
        {
            if (author == null || id == Guid.Empty)
            {
                return BadRequest(new ApiBadRequestResponse("author information is null or invalid id found"));
            }

            try
            {
                var currentAuthor = await _authorManager.GetAuthorByIdAsync(id);
                if (currentAuthor == null)
                {
                    return BadRequest($"The author {id} cannot be found.");
                }

                currentAuthor = _mapper.Map(author, currentAuthor);
                var newAuthor = await _authorManager.UpdateAsync(currentAuthor);
                if (newAuthor == null)
                {
                    return BadRequest(_authorManager.ProcessResult.MessageList);
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PATCH api/authors/5
        [HttpPatch("{id:guid}", Name = "PatchAuthor")]
        public async Task<IActionResult> Patch(Guid id, JsonPatchDocument<ApiAuthorForUpdate> patchDoc)
        {
            if (patchDoc == null || id == Guid.Empty)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var curUsr =await _authorManager.GetAuthorByIdAsync(id); 
                if (curUsr == null)
                {
                    return NotFound();
                }

                var author2Update = _mapper.Map<ApiAuthorForUpdate>(curUsr);
                patchDoc.ApplyTo(author2Update);
                _mapper.Map(author2Update, curUsr);

                var newAuthor = await _authorManager.UpdateAsync(curUsr);
                if (newAuthor == null)
                {
                    return BadRequest(_authorManager.ProcessResult.MessageList);
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // DELETE api/authors/5
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var authorExists = await _authorManager.AuthorExistsAsync(id.ToString());
            if (!authorExists)
            {
                return NotFound($"The author: {id} cannot found");
            }

            try
            {
                await _authorManager.DeActivateAsync(id);
                if (_authorManager.ProcessResult.Success)
                {
                    return NoContent();
                }

                throw new Exception($"Deleting author {id} failed on saving");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}