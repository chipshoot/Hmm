using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.Infrastructure;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Serilog;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Controllers
{
    [Route("api/authors")]
    [ApiController]
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

        [HttpGet("{subject}", Name = "GetAuthorBySubject")]
        public async Task<IActionResult> Get(string subject)
        {
            var id = new Guid(subject);
            var author = _authorManager.GetEntities().FirstOrDefault(u => u.Id == id);
            if (author == null)
            {
                // subject must come from token
                var subjectFromToken = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                if (subjectFromToken == null)
                {
                    var error = "null user subject found";
                    Log.Logger.Error(error);
                    return BadRequest(new ApiBadRequestResponse(error));
                }

                var httpClient = _httpClientFactory.CreateClient(HmmServiceApiConstants.HttpClient.Idp);
                var userName = await IdpUserProfileProvider.GetUserClaimAsync("name", HttpContext, httpClient);
                if (string.IsNullOrEmpty(userName))
                {
                    Log.Logger.Error($"Cannot get username for subject {subjectFromToken}");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }

                var authorToCreate = new Author
                {
                    Id = new Guid(subjectFromToken),
                    AccountName = userName,
                    Role = AuthorRoleType.Guest,
                    IsActivated = true
                };

                author = _authorManager.Create(authorToCreate);
            }

            var ret = _mapper.Map<Author, ApiAuthor>(author);
            return Ok(ret);
        }

        // POST api/authors
        [HttpPost]
        public IActionResult CreateAuthor(ApiAuthorForCreate author)
        {
            try
            {
                var subject = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                if (string.IsNullOrEmpty(subject))
                {
                    return BadRequest();
                }

                if (_authorManager.AuthorExists(subject))
                {
                    return BadRequest();
                }

                var usr = _mapper.Map<ApiAuthorForCreate, Author>(author);
                usr.Id = new Guid(subject);
                usr.IsActivated = true;
                var newAuthor = _authorManager.Create(usr);

                if (newAuthor == null)
                {
                    return BadRequest(new ApiBadRequestResponse("null author found"));
                }

                var apiNewAuthor = _mapper.Map<Author, ApiAuthor>(newAuthor);

                return Ok(apiNewAuthor);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // PUT api/authors/5
        [HttpPut("{id}")]
        public IActionResult Put(Guid id, ApiAuthorForUpdate author)
        {
            if (author == null || id != Guid.Empty)
            {
                return BadRequest(new ApiBadRequestResponse("author information is null or invalid id found"));
            }

            try
            {
                var currentAuthor = _authorManager.GetEntities().FirstOrDefault(u => u.Id == id);
                if (currentAuthor == null)
                {
                    return NotFound();
                }

                currentAuthor = _mapper.Map(author, currentAuthor);
                var apiNewAuthor = _authorManager.Update(currentAuthor);
                if (apiNewAuthor == null)
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
        [HttpPatch("{id}")]
        public IActionResult Patch(Guid id, JsonPatchDocument<ApiAuthorForUpdate> patchDoc)
        {
            if (patchDoc == null || id != Guid.Empty)
            {
                return BadRequest(new ApiBadRequestResponse("Patch information is null or invalid id found"));
            }

            try
            {
                var curUsr = _authorManager.GetEntities().FirstOrDefault(u => u.Id == id);
                if (curUsr == null)
                {
                    return NotFound();
                }

                var author2Update = _mapper.Map<ApiAuthorForUpdate>(curUsr);
                patchDoc.ApplyTo(author2Update);
                _mapper.Map(author2Update, curUsr);

                var newAuthor = _authorManager.Update(curUsr);
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
        [HttpDelete("{id}")]
        public ActionResult Delete(Guid id)
        {
            _authorManager.DeActivate(id);
            if (_authorManager.ProcessResult.Success)
            {
                return NoContent();
            }

            if (_authorManager.ProcessResult.MessageList.Contains($"Cannot find author with id : {id}"))
            {
                return NotFound();
            }

            throw new Exception($"Deleting author {id} failed on saving");
        }
    }
}
