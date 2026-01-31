using Hmm.Core;
using Hmm.Core.DefaultManager;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.Areas.HmmNoteService.Controllers;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using Hmm.Utility.Validation;
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
        private readonly Mock<IHmmValidator<Author>> _mockValidator;

        public AuthorControllerTests()
        {
            _mockValidator = new Mock<IHmmValidator<Author>>();
            _mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<Author>()))
                .ReturnsAsync(ProcessingResult<Author>.Ok(It.IsAny<Author>()));
            _authorManager = new AuthorManager(AuthorRepository, UnitOfWork, Mapper, LookupRepository, _mockValidator.Object);
            _controller = new AuthorController(_authorManager, ApiMapper, new Mock<ILogger<AuthorController>>().Object);

            // Set up HttpContext for the controller
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        private AuthorController CreateControllerWithMockedManager(Mock<IAuthorManager> mockManager)
        {
            var controller = new AuthorController(mockManager.Object, ApiMapper, new Mock<ILogger<AuthorController>>().Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
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
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal("No authors found.", problemDetails.Detail);
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
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal($"The author {authorId} cannot be found.", problemDetails.Detail);
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
            var createdResult = Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Equal("GetAuthorById", createdResult.RouteName);
            Assert.Equal("1.0", createdResult.RouteValues?["version"]);
            var returnAuthor = Assert.IsType<Author>(createdResult.Value);
            Assert.Equal(author.Id, returnAuthor.Id);
            Assert.Equal(returnAuthor.Id, createdResult.RouteValues?["id"]);
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
            var controller = CreateControllerWithMockedManager(mockAuthorManager);

            // Act
            var result = await controller.Post(apiAuthor);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("An unexpected error occurred while creating the author.", problemDetails.Detail);
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
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Equal("Author information is null or invalid id found", problemDetails.Detail);
        }

        [Fact]
        public async Task UpdateAuthor_ReturnsNotFound_WhenAuthorNotFound()
        {
            // Arrange
            const int authorId = 1000;
            var apiAuthorForUpdate = new ApiAuthorForUpdate { AccountName = "UpdatedAuthor" };

            // Act
            var result = await _controller.Put(authorId, apiAuthorForUpdate);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal($"Author with id {authorId} not found", problemDetails.Detail);
        }

        [Fact]
        public async Task UpdateAuthor_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            const int authorId = 100;
            var apiAuthorForUpdate = new ApiAuthorForUpdate { AccountName = "UpdatedAuthor" };
            var mockAuthorManager = new Mock<IAuthorManager>();
            mockAuthorManager.Setup(m => m.UpdateAsync(It.IsAny<Author>()))
                .ReturnsAsync(ProcessingResult<Author>.Invalid("Validation error occurred"));
            var controller = CreateControllerWithMockedManager(mockAuthorManager);

            // Act
            var result = await controller.Put(authorId, apiAuthorForUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Equal("Validation error occurred", problemDetails.Detail);
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
            var controller = CreateControllerWithMockedManager(mockAuthorManager);

            // Act
            var result = await controller.Put(authorId, apiAuthorForUpdate);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("An unexpected error occurred while updating the author.", problemDetails.Detail);
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
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Equal("Patch information is null or invalid id found", problemDetails.Detail);
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
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal($"Author with id {authorId} not found", problemDetails.Detail);
        }

        [Fact]
        public async Task PatchAuthor_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            const int authorId = 100;
            var patchDoc = new JsonPatchDocument<ApiAuthorForUpdate>();
            patchDoc.Replace(e => e.AccountName, "SomeNewName");

            var mockAuthorManager = new Mock<IAuthorManager>();
            mockAuthorManager.Setup(a => a.GetAuthorByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => ProcessingResult<Author>.Ok(new Author { Id = id, AccountName = "ExistsAuthor" }));
            mockAuthorManager.Setup(m => m.UpdateAsync(It.IsAny<Author>()))
                .ReturnsAsync(ProcessingResult<Author>.Invalid("Validation error occurred"));
            var controller = CreateControllerWithMockedManager(mockAuthorManager);

            // Act
            var result = await controller.Patch(authorId, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Equal("Validation error occurred", problemDetails.Detail);
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
            var controller = CreateControllerWithMockedManager(mockAuthorManager);

            // Act
            var result = await controller.Patch(authorId, patchDoc);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("An unexpected error occurred while patching the author.", problemDetails.Detail);
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
        public async Task Delete_ReturnsNotFound_WhenIdIsInvalid()
        {
            // Act
            var result = await _controller.Delete(0);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal("Author with id 0 not found", problemDetails.Detail);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenAuthorNotFound()
        {
            // Arrange
            const int authorId = 1;

            // Act
            var result = await _controller.Delete(authorId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal($"Author with id {authorId} not found", problemDetails.Detail);
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
            var controller = CreateControllerWithMockedManager(mockAuthorManager);

            // Act
            var result = await controller.Delete(authorId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("An unexpected error occurred while deactivating the author.", problemDetails.Detail);
        }

        #endregion Delete Author
    }
}