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
    public class TagControllerTests : CoreTestFixtureBase
    {
        private readonly TagManager _tagManager;
        private readonly TagController _controller;
        private readonly Mock<IHmmValidator<Tag>> _mockValidator;

        public TagControllerTests()
        {
            _mockValidator = new Mock<IHmmValidator<Tag>>();
            _mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<Tag>()))
                .ReturnsAsync(ProcessingResult<Tag>.Ok(It.IsAny<Tag>()));
            _tagManager = new TagManager(TagRepository, UnitOfWork, Mapper, LookupRepository, _mockValidator.Object);
            _controller = new TagController(_tagManager, ApiMapper, new Mock<ILogger<TagController>>().Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            ConfigureControllerForValidation(_controller);
        }

        private TagController CreateControllerWithMockedManager(Mock<ITagManager> mockManager)
        {
            var controller = new TagController(mockManager.Object, ApiMapper, new Mock<ILogger<TagController>>().Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            ConfigureControllerForValidation(controller);
            return controller;
        }

        #region Get tag by Id

        [Fact]
        public async Task Get_ReturnsOkResult_WithListOfTags()
        {
            // Arrange
            // Act
            var result = await _controller.Get(new ResourceCollectionParameters());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnTags = Assert.IsType<PageList<Tag>>(okResult.Value);
            Assert.Equal(4, returnTags.Count);
        }

        [Fact]
        public async Task Get_ReturnsOkResult_WithEmptyList_WhenNoTagFound()
        {
            // Arrange
            ResetDataSource(ElementType.Tag);
            // Act
            var result = await _controller.Get(new ResourceCollectionParameters());

            // Assert - REST best practice: empty collection returns 200 OK with empty array
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnTags = Assert.IsType<PageList<Tag>>(okResult.Value);
            Assert.Empty(returnTags);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(101)]
        [InlineData(102)]
        [InlineData(103)]
        public async Task GetTagById_ReturnsOkResult_WithTag(int tagId)
        {
            // Arrange
            // Act
            var result = await _controller.Get(tagId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnTag = Assert.IsType<Tag>(okResult.Value);
            Assert.Equal(tagId, returnTag.Id);
        }

        [Fact]
        public async Task GetTagById_ReturnsNotFound_WhenTagNotFound()
        {
            // Arrange
            var tagId = 20;
            // Act
            var result = await _controller.Get(tagId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal($"The tag : {tagId} not found.", problemDetails.Detail);
        }

        #endregion Get tag by Id

        #region Get tag by name

        [Fact]
        public async Task GetTagByName_ReturnsOkResult_WithTag()
        {
            // Arrange
            var existingTagResult = await _tagManager.GetTagByIdAsync(100);
            Assert.True(existingTagResult.Success);
            var existingTag = existingTagResult.Value;

            // Act
            var result = await _controller.GetByName(existingTag.Name);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnTag = Assert.IsType<Tag>(okResult.Value);
            Assert.Equal(existingTag.Name, returnTag.Name);
        }

        [Fact]
        public async Task GetTagByName_ReturnsNotFound_WhenTagNotFound()
        {
            // Arrange
            const string tagName = "NonExistentTag";

            // Act
            var result = await _controller.GetByName(tagName);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal($"The tag with name '{tagName}' not found.", problemDetails.Detail);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetTagByName_ReturnsBadRequest_WhenNameIsEmptyOrWhitespace(string name)
        {
            // Act
            var result = await _controller.GetByName(name);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Equal("Tag name is required", problemDetails.Detail);
        }

        [Fact]
        public async Task GetTagByName_ReturnsBadRequest_WhenNameIsNull()
        {
            // Act
            var result = await _controller.GetByName(null!);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Equal("Tag name is required", problemDetails.Detail);
        }

        #endregion Get tag by name

        #region Add a new tag

        [Fact]
        public async Task AddTag_ReturnsCreatedResult_WithNewTag()
        {
            // Arrange
            var apiTag = new ApiTagForCreate { Name = "TestTag" };
            var tag = new Tag { Id = 5, Name = "TestTag" };

            // Act
            var result = await _controller.Post(apiTag);

            // Assert
            var createdResult = Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Equal("GetTagById", createdResult.RouteName);
            Assert.Equal("1.0", createdResult.RouteValues?["version"]);
            var returnTag = Assert.IsType<Tag>(createdResult.Value);
            Assert.Equal(tag.Id, returnTag.Id);
            Assert.Equal(returnTag.Id, createdResult.RouteValues?["id"]);
        }

        [Fact]
        public async Task AddTag_ReturnsBadRequest_WhenCreationFails()
        {
            // Arrange
            var apiTag = new ApiTagForCreate { Name = "TestTag" };

            // Set up validator to return validation error
            _mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<Tag>()))
                .ReturnsAsync(ProcessingResult<Tag>.Invalid("Tag with name 'TestTag' already exists"));

            // Act
            var result = await _controller.Post(apiTag);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddTag_ReturnsInternalServerError_OnException()
        {
            // Arrange
            var apiTag = new ApiTagForCreate { Name = "TestTag" };
            var mockTagManager = new Mock<ITagManager>();
            mockTagManager.Setup(m => m.CreateAsync(It.IsAny<Tag>())).Throws(new Exception());
            var controller = CreateControllerWithMockedManager(mockTagManager);

            // Act
            var result = await controller.Post(apiTag);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("An unexpected error occurred while creating the tag.", problemDetails.Detail);
        }

        #endregion Add a new tag

        #region Update tag

        [Fact]
        public async Task UpdateTag_ReturnsNoContent_WhenUpdateIsSuccessful()
        {
            // Arrange
            const int tagId = 100;
            var existingTagResult = await _tagManager.GetTagByIdAsync(tagId);
            var existingTag = existingTagResult.Value;
            Assert.NotNull(existingTag);
            var apiTagForUpdate = new ApiTagForUpdate { Name = "UpdatedTag" };

            // Act
            var result = await _controller.Put(tagId, apiTagForUpdate);
            var updatedTagResult = await _tagManager.GetTagByIdAsync(tagId);
            var updatedTag = updatedTagResult.Value;

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(tagId, updatedTag.Id);
            Assert.Equal(apiTagForUpdate.Name, updatedTag.Name);
        }

        [Fact]
        public async Task UpdateTag_ReturnsBadRequest_WhenTagIsNullOrInvalidId()
        {
            // Arrange
            ApiTagForUpdate? apiTagForUpdate = null;

            // Act
            var result = await _controller.Put(0, apiTagForUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Equal("Tag information is null or invalid id found", problemDetails.Detail);
        }

        [Fact]
        public async Task UpdateTag_ReturnsNotFound_WhenTagNotFound()
        {
            // Arrange
            var tagId = 1000;
            var apiTagForUpdate = new ApiTagForUpdate { Name = "UpdatedTag" };

            // Act
            var result = await _controller.Put(tagId, apiTagForUpdate);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal($"Tag with id {tagId} not found", problemDetails.Detail);
        }

        [Fact]
        public async Task UpdateTag_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            var existingTagResult = await _tagManager.GetTagByIdAsync(100);
            var existingTag = existingTagResult.Value;
            var existingTag2Result = await _tagManager.GetTagByIdAsync(101);
            var existingTag2 = existingTag2Result.Value;
            var apiTagForUpdate = new ApiTagForUpdate { Name = existingTag2.Name };

            // Set up validator to return validation error for duplicate name
            _mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<Tag>()))
                .ReturnsAsync(ProcessingResult<Tag>.Invalid($"Tag with name '{existingTag2.Name}' already exists"));

            // Act
            var result = await _controller.Put(existingTag.Id, apiTagForUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Contains($"Tag with name '{existingTag2.Name}' already exists", problemDetails.Detail);
        }

        [Fact]
        public async Task UpdateTag_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int tagId = 100;
            var apiTagForUpdate = new ApiTagForUpdate { Name = "UpdatedTag" };
            var mockTagManager = new Mock<ITagManager>();
            mockTagManager.Setup(a => a.GetTagByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => ProcessingResult<Tag>.Ok(new Tag { Id = id, Name = "Exists Tag" }));
            mockTagManager.Setup(m => m.UpdateAsync(It.IsAny<Tag>())).Throws(new Exception());
            var controller = CreateControllerWithMockedManager(mockTagManager);

            // Act
            var result = await controller.Put(tagId, apiTagForUpdate);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("An unexpected error occurred while updating the tag.", problemDetails.Detail);
        }

        #endregion Update tag

        #region Patch tag

        [Fact]
        public async Task PatchTag_ReturnsNoContent_WhenPatchIsSuccessful()
        {
            // Arrange
            const int tagId = 100;
            var patchDoc = new JsonPatchDocument<ApiTagForUpdate>();
            var existingTagResult = await _tagManager.GetTagByIdAsync(tagId);
            var existingTag = existingTagResult.Value;
            Assert.NotNull(existingTag);
            patchDoc.Replace(e => e.Description, "UpdatedTag with new description");

            // Act
            var result = await _controller.Patch(tagId, patchDoc);
            var updatedTagResult = await _tagManager.GetTagByIdAsync(tagId);
            var updatedTag = updatedTagResult.Value;

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal("UpdatedTag with new description", updatedTag.Description);
        }

        [Fact]
        public async Task PatchTag_ReturnsBadRequest_WhenPatchDocIsNullOrInvalidId()
        {
            // Arrange
            JsonPatchDocument<ApiTagForUpdate>? patchDoc = null;

            // Act
            var result = await _controller.Patch(0, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Equal("Patch information is null or invalid id found", problemDetails.Detail);
        }

        [Fact]
        public async Task PatchTag_ReturnsNotFound_WhenTagNotFound()
        {
            // Arrange
            const int tagId = 1;
            var patchDoc = new JsonPatchDocument<ApiTagForUpdate>();

            // Act
            var result = await _controller.Patch(tagId, patchDoc);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal($"Tag with id {tagId} not found", problemDetails.Detail);
        }

        [Fact]
        public async Task PatchTag_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            const int tagId = 100;
            var patchDoc = new JsonPatchDocument<ApiTagForUpdate>();
            var existingTagResult = await _tagManager.GetTagByIdAsync(tagId);
            var existingTag = existingTagResult.Value;
            patchDoc.Replace(e => e.Name, existingTag.Name);

            // Set up validator to return validation error for duplicate name
            _mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<Tag>()))
                .ReturnsAsync(ProcessingResult<Tag>.Invalid($"Tag with name '{existingTag.Name}' already exists"));

            // Act
            var result = await _controller.Patch(tagId + 1, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Contains($"Tag with name '{existingTag.Name}' already exists", problemDetails.Detail);
        }

        [Fact]
        public async Task PatchTag_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int tagId = 100;
            var patchDoc = new JsonPatchDocument<ApiTagForUpdate>();
            patchDoc.Replace(e => e.Name, "SomeNewName");

            var mockTagManager = new Mock<ITagManager>();
            mockTagManager.Setup(a => a.GetTagByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => ProcessingResult<Tag>.Ok(new Tag { Id = id, Name = "Exists Tag" }));
            mockTagManager.Setup(m => m.UpdateAsync(It.IsAny<Tag>())).Throws(new Exception());
            var controller = CreateControllerWithMockedManager(mockTagManager);

            // Act
            var result = await controller.Patch(tagId, patchDoc);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("An unexpected error occurred while patching the tag.", problemDetails.Detail);
        }

        [Fact]
        public async Task PatchTag_ReturnsBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            const int tagId = 100;
            var patchDoc = new JsonPatchDocument<ApiTagForUpdate>();
            // Set Name to null which violates the [Required] validation
            patchDoc.Replace(e => e.Name, null);

            // Act
            var result = await _controller.Patch(tagId, patchDoc);

            // Assert - should return BadRequest because Name is required
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Equal("Validation Error", problemDetails.Title);
            Assert.Contains("Name", problemDetails.Detail);
        }

        #endregion Patch tag

        #region Delete tag

        [Fact]
        public async Task Delete_ReturnsNoContent_WhenDeleteIsSuccessful()
        {
            // Arrange
            const int tagId = 100;
            var existingTagResult = await _tagManager.GetTagByIdAsync(tagId);
            Assert.True(existingTagResult.Success);
            Assert.NotNull(existingTagResult.Value);

            // Act
            var result = await _controller.Delete(tagId);
            var deletedTagResult = await _tagManager.GetTagByIdAsync(tagId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.False(deletedTagResult.Success);
            Assert.Equal(ErrorCategory.Deleted, deletedTagResult.ErrorType);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenIdIsInvalid()
        {
            // Act
            var result = await _controller.Delete(0);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal("Tag with id 0 not found", problemDetails.Detail);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenTagNotFound()
        {
            // Arrange
            const int tagId = 1;

            // Act
            var result = await _controller.Delete(tagId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal($"Tag with id {tagId} not found", problemDetails.Detail);
        }

        [Fact]
        public async Task Delete_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int tagId = 1;
            var mockTagManager = new Mock<ITagManager>();
            mockTagManager.Setup(a => a.GetTagByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => ProcessingResult<Tag>.Ok(new Tag { Id = id, Name = "Exists Tag" }));
            mockTagManager.Setup(a => a.IsTagExistsAsync(It.IsAny<int>())).ReturnsAsync(true);
            mockTagManager.Setup(m => m.DeActivateAsync(It.IsAny<int>())).Throws(new Exception());
            var controller = CreateControllerWithMockedManager(mockTagManager);

            // Act
            var result = await controller.Delete(tagId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("An unexpected error occurred while deactivating the tag.", problemDetails.Detail);
        }

        #endregion Delete tag
    }
}