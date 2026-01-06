using Hmm.Core;
using Hmm.Core.DefaultManager;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.Areas.HmmNoteService.Controllers;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hmm.ServiceApi.Core.Tests
{
    public class AuthorControllerTests : CoreTestFixtureBase
    {
        private readonly AuthorManager _authorManager;
        private readonly AuthorController _controller;

        public AuthorControllerTests()
        {
            _authorManager = new AuthorManager(AuthorRepository, Mapper, LookupRepository);
            _controller = new AuthorController(_authorManager, ApiMapper, new Mock<ILogger<AuthorController>>().Object);
        }

        #region Get author by Id

        [Fact]
        public async Task Get_ReturnsOkResult_WithListOfAuthors()
        {
            // Arrange
            // Act
            var result = await _controller.Get(new ResourceCollectionParameters());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnAuthors = Assert.IsType<PageList<Author>>(okResult.Value);
            Assert.Equal(4, returnAuthors.Count);
        }

        [Fact]
        public async Task Get_ReturnsNotFound_WhenNoAuthorFound()
        {
            // Arrange
            ResetDataSource(ElementType.Author);
            // Act
            var result = await _controller.Get(new ResourceCollectionParameters());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(101)]
        [InlineData(102)]
        [InlineData(103)]
        public async Task GetAuthorById_ReturnsOkResult_WithAuthor(int authorId)
        {
            // Arrange
            // Act
            var result = await _controller.Get(authorId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnAuthor = Assert.IsType<Author>(okResult.Value);
            Assert.Equal(authorId, returnAuthor.Id);
        }

        [Fact]
        public async Task GetAuthorById_ReturnsNotFound_WhenAuthorNotFound()
        {
            // Arrange
            const int authorId = 20;
            // Act
            var result = await _controller.Get(authorId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"The author {authorId} cannot be found.", notFoundResult.Value);
        }

        #endregion Get Author by Id

        #region Add a new Author

        [Fact]
        public async Task AddAuthor_ReturnsCreatedResult_WithNewAuthor()
        {
            // Arrange
            var apiAuthor = new ApiAuthorForCreate { AccountName = "TestAuthor" };
            var author = new Author { Id = 5, AccountName = "TestAuthor" };

            // Act
            var result = await _controller.Post(apiAuthor);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            var returnAuthor = Assert.IsType<Author>(createdResult.Value);
            Assert.Equal(author.Id, returnAuthor.Id);
        }

        [Fact]
        public async Task AddAuthor_ReturnsBadRequest_WhenCreationFails()
        {
            // Arrange
            var apiAuthor = new ApiAuthorForCreate { AccountName = "TestAuthor" };
            var author = new Author { Id = 5, AccountName = "TestAuthor" };
            InsertAuthor(author);

            // Act
            var result = await _controller.Post(apiAuthor);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddAuthor_ReturnsInternalServerError_OnException()
        {
            // Arrange
            var apiAuthor = new ApiAuthorForCreate { AccountName = "TestAuthor" };
            var mockAuthorManager = new Mock<IAuthorManager>();
            mockAuthorManager.Setup(m => m.CreateAsync(It.IsAny<Author>())).Throws(new Exception());
            var controller = new AuthorController(mockAuthorManager.Object, ApiMapper, new Mock<ILogger<AuthorController>>().Object);

            // Act
            var result = await controller.Post(apiAuthor);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion Add a new author

        #region Update author

        [Fact]
        public async Task UpdateAuthor_ReturnsNoContent_WhenUpdateIsSuccessful()
        {
            // Arrange
            const int authorId = 100;
            var existingAuthorResult = await _authorManager.GetAuthorByIdAsync(authorId);
            Assert.True(existingAuthorResult.Success);
            var existingAuthor = existingAuthorResult.Value;
            Assert.NotNull(existingAuthor);
            var apiAuthorForUpdate = ApiMapper.Map<ApiAuthorForUpdate>(existingAuthor);
            apiAuthorForUpdate.AccountName = "UpdatedAuthor";

            // Act
            var result = await _controller.Put(authorId, apiAuthorForUpdate);
            var updatedAuthorResult = await _authorManager.GetAuthorByIdAsync(authorId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.True(updatedAuthorResult.Success);
            Assert.Equal(authorId, updatedAuthorResult.Value.Id);
            Assert.Equal(apiAuthorForUpdate.AccountName, updatedAuthorResult.Value.AccountName);
        }

        [Fact]
        public async Task UpdateAuthor_ReturnsBadRequest_WhenAuthorIsNullOrInvalidId()
        {
            // Arrange
            ApiAuthorForUpdate? apiAuthorForUpdate = null;

            // Act
            var result = await _controller.Put(0, apiAuthorForUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Author information is null or invalid id found", (badRequestResult.Value as ApiBadRequestResponse)?.Errors.FirstOrDefault());
            Assert.Equal("Bad request data", (badRequestResult.Value as ApiBadRequestResponse)?.Message);
        }

        [Fact]
        public async Task UpdateAuthor_ReturnsBadRequest_WhenAuthorNotFound()
        {
            // Arrange
            const int authorId = 1000;
            var apiAuthorForUpdate = new ApiAuthorForUpdate { AccountName = "UpdatedAuthor" };

            // Act
            var result = await _controller.Put(authorId, apiAuthorForUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var msg = (badRequestResult.Value as List<ReturnMessage>)?[0].Message;
            Assert.Equal("Cannot update author: UpdatedAuthor, because system cannot find it in data source", msg);
        }

        [Fact]
        public async Task UpdateAuthor_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            var existingAuthorResult = await _authorManager.GetAuthorByIdAsync(100);
            Assert.True(existingAuthorResult.Success);
            var existingAuthor = existingAuthorResult.Value;
            
            var existingAuthor2Result = await _authorManager.GetAuthorByIdAsync(101);
            Assert.True(existingAuthor2Result.Success);
            var existingAuthor2 = existingAuthor2Result.Value;
            
            var apiAuthorForUpdate = new ApiAuthorForUpdate { AccountName = existingAuthor2.AccountName };

            // Act
            var result = await _controller.Put(existingAuthor.Id, apiAuthorForUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResponse = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.NotEmpty(apiResponse.Errors);
        }

        [Fact]
        public async Task UpdateAuthor_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int authorId = 100;
            var apiAuthorForUpdate = new ApiAuthorForUpdate { AccountName = "UpdatedAuthor" };
            var mockAuthorManager = new Mock<IAuthorManager>();
            mockAuthorManager.Setup(a => a.GetAuthorByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => ProcessingResult<Author>.Ok(new Author { Id = id, AccountName = "Exists Author" }));
            mockAuthorManager.Setup(m => m.UpdateAsync(It.IsAny<Author>())).Throws(new Exception());
            var controller = new AuthorController(mockAuthorManager.Object, ApiMapper, new Mock<ILogger<AuthorController>>().Object);

            // Act
            var result = await controller.Put(authorId, apiAuthorForUpdate);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion Update author

        #region Patch author

        [Fact]
        public async Task PatchAuthor_ReturnsNoContent_WhenPatchIsSuccessful()
        {
            // Arrange
            const int authorId = 100;
            var patchDoc = new JsonPatchDocument<ApiAuthorForUpdate>();
            var existingAuthorResult = await _authorManager.GetAuthorByIdAsync(authorId);
            Assert.True(existingAuthorResult.Success);
            var existingAuthor = existingAuthorResult.Value;
            Assert.NotNull(existingAuthor);
            patchDoc.Replace(e => e.Description, "Updated author with new description");
            patchDoc.Replace(e => e.ContactInfo.FirstName, "Updated author contact first name");

            // Act
            var result = await _controller.Patch(authorId, patchDoc);
            var updatedAuthorResult = await _authorManager.GetAuthorByIdAsync(authorId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.True(updatedAuthorResult.Success);
            Assert.Equal("Updated author with new description", updatedAuthorResult.Value.Description);
            Assert.Equal("Updated author contact first name", updatedAuthorResult.Value.ContactInfo.FirstName);
        }

        [Fact]
        public async Task PatchAuthor_ReturnsBadRequest_WhenPatchDocIsNullOrInvalidId()
        {
            // Arrange
            JsonPatchDocument<ApiAuthorForUpdate>? patchDoc = null;

            // Act
            var result = await _controller.Patch(0, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Patch information is null or invalid id found", (badRequestResult.Value as ApiBadRequestResponse)?.Errors.FirstOrDefault());
            Assert.Equal("Bad request data", (badRequestResult.Value as ApiBadRequestResponse)?.Message);
        }

        [Fact]
        public async Task PatchAuthor_ReturnsNotFound_WhenAuthorNotFound()
        {
            // Arrange
            const int authorId = 1;
            var patchDoc = new JsonPatchDocument<ApiAuthorForUpdate>();

            // Act
            var result = await _controller.Patch(authorId, patchDoc);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task PatchAuthor_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            const int authorId = 100;
            var patchDoc = new JsonPatchDocument<ApiAuthorForUpdate>();
            var existingAuthorResult = await _authorManager.GetAuthorByIdAsync(authorId);
            Assert.True(existingAuthorResult.Success);
            var existingAuthor = existingAuthorResult.Value;
            patchDoc.Replace(e => e.AccountName, existingAuthor.AccountName);

            // Act
            var result = await _controller.Patch(authorId + 1, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResponse = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.NotEmpty(apiResponse.Errors);
        }

        [Fact]
        public async Task PatchAuthor_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int authorId = 100;
            var patchDoc = new JsonPatchDocument<ApiAuthorForUpdate>();
            patchDoc.Replace(e => e.AccountName, "SomeNewName");

            var mockAuthorManager = new Mock<IAuthorManager>();
            mockAuthorManager.Setup(a => a.GetAuthorByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => ProcessingResult<Author>.Ok(new Author { Id = id, AccountName = "ExistsAuthor" }));
            mockAuthorManager.Setup(m => m.UpdateAsync(It.IsAny<Author>())).Throws(new Exception());
            var controller = new AuthorController(mockAuthorManager.Object, ApiMapper, new Mock<ILogger<AuthorController>>().Object);

            // Act
            var result = await controller.Patch(authorId, patchDoc);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion Patch author

        #region Delete author

        [Fact]
        public async Task Delete_ReturnsNoContent_WhenDeleteIsSuccessful()
        {
            // Arrange
            const int authorId = 100;
            var existingAuthorResult = await _authorManager.GetAuthorByIdAsync(authorId);
            Assert.True(existingAuthorResult.Success);
            Assert.NotNull(existingAuthorResult.Value);

            // Act
            var result = await _controller.Delete(authorId);
            var deletedAuthorResult = await _authorManager.GetAuthorByIdAsync(authorId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.False(deletedAuthorResult.Success);
        }

        [Fact]
        public async Task Delete_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Act
            var result = await _controller.Delete(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid author id found.", badRequestResult.Value);
        }

        [Fact]
        public async Task Delete_ReturnsBadRequest_WhenAuthorNotFound()
        {
            // Arrange
            const int authorId = 1;

            // Act
            var result = await _controller.Delete(authorId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal($"Invalid author id found.", badRequestResult.Value);
        }

        [Fact]
        public async Task Delete_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int authorId = 100;
            var mockAuthorManager = new Mock<IAuthorManager>();
            mockAuthorManager.Setup(a => a.GetAuthorByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => ProcessingResult<Author>.Ok(new Author { Id = id, AccountName = "ExistsAuthor" }));
            mockAuthorManager.Setup(a => a.IsAuthorExistsAsync(It.IsAny<int>())).ReturnsAsync(true);
            mockAuthorManager.Setup(m => m.DeActivateAsync(It.IsAny<int>())).Throws(new Exception());
            var controller = new AuthorController(mockAuthorManager.Object, ApiMapper, new Mock<ILogger<AuthorController>>().Object);

            // Act
            var result = await controller.Delete(authorId);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion Delete Author
    }
}