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
using Moq;

namespace Hmm.ServiceApi.Core.Tests
{
    public class TagControllerTests : CoreTestFixtureBase
    {
        private readonly TagManager _tagManager;
        private readonly TagController _controller;

        public TagControllerTests()
        {
            _tagManager = new TagManager(TagRepository, Mapper, LookupRepository);
            _controller = new TagController(_tagManager, ApiMapper);
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
        public async Task Get_ReturnsNotFound_WhenNoTagFound()
        {
            // Arrange
            ResetDataSource(ElementType.Tag);
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
            Assert.Equal($"The tag : {tagId} not found.", notFoundResult.Value);
        }

        #endregion Get tag by Id

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
            var createdResult = Assert.IsType<CreatedResult>(result);
            var returnTag = Assert.IsType<Tag>(createdResult.Value);
            Assert.Equal(tag.Id, returnTag.Id);
        }

        [Fact]
        public async Task AddTag_ReturnsBadRequest_WhenCreationFails()
        {
            // Arrange
            var apiTag = new ApiTagForCreate { Name = "TestTag" };
            var tag = new Tag { Id = 5, Name = "TestTag" };
            InsertTag(tag);

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
            var controller = new TagController(mockTagManager.Object, ApiMapper);

            // Act
            var result = await controller.Post(apiTag);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
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
            Assert.Equal("Tag information is null or invalid id found", (badRequestResult.Value as ApiBadRequestResponse)?.Errors.FirstOrDefault());
            Assert.Equal("Bad request data", (badRequestResult.Value as ApiBadRequestResponse)?.Message);
        }

        [Fact]
        public async Task UpdateTag_ReturnsBadRequest_WhenTagNotFound()
        {
            // Arrange
            var tagId = 1000;
            var apiTagForUpdate = new ApiTagForUpdate { Name = "UpdatedTag" };

            // Act
            var result = await _controller.Put(tagId, apiTagForUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal($"Tag {tagId} cannot be found.", badRequestResult.Value);
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

            // Act
            var result = await _controller.Put(existingTag.Id, apiTagForUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResponse = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.NotEmpty(apiResponse.Errors);
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
            var controller = new TagController(mockTagManager.Object, ApiMapper);

            // Act
            var result = await controller.Put(tagId, apiTagForUpdate);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
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
            Assert.Equal("Patch information is null or invalid id found", (badRequestResult.Value as ApiBadRequestResponse)?.Errors.FirstOrDefault());
            Assert.Equal("Bad request data", (badRequestResult.Value as ApiBadRequestResponse)?.Message);
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
            Assert.IsType<NotFoundResult>(result);
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

            // Act
            var result = await _controller.Patch(tagId + 1, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResponse = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.NotEmpty(apiResponse.Errors);
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
            var controller = new TagController(mockTagManager.Object, ApiMapper);

            // Act
            var result = await controller.Patch(tagId, patchDoc);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion Patch tag

        #region Delete tag

        [Fact]
        public async Task Delete_ReturnsNoContent_WhenDeleteIsSuccessful()
        {
            // Arrange
            const int tagId = 100;
            var existingTag = await _tagManager.GetTagByIdAsync(tagId);
            Assert.NotNull(existingTag);

            // Act
            var result = await _controller.Delete(tagId);
            var deletedTag = await _tagManager.GetTagByIdAsync(tagId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(deletedTag);
        }

        [Fact]
        public async Task Delete_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Act
            var result = await _controller.Delete(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid tag id found", (badRequestResult.Value as ApiBadRequestResponse)?.Errors.FirstOrDefault());
        }

        [Fact]
        public async Task Delete_ReturnsBadRequest_WhenTagNotFound()
        {
            // Arrange
            const int tagId = 1;

            // Act
            var result = await _controller.Delete(tagId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal($"Tag {tagId} cannot be found.", badRequestResult.Value);
        }

        [Fact]
        public async Task Delete_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int tagId = 1;
            var mockTagManager = new Mock<ITagManager>();
            mockTagManager.Setup(a => a.GetTagByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => ProcessingResult<Tag>.Ok(new Tag { Id = id, Name = "Exists Tag" }));
            mockTagManager.Setup(m => m.DeActivateAsync(It.IsAny<int>())).Throws(new Exception());
            var controller = new TagController(mockTagManager.Object, ApiMapper);

            // Act
            var result = await controller.Delete(tagId);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion Delete tag
    }
}