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
using NoteContentFormatType = Hmm.ServiceApi.DtoEntity.HmmNote.NoteContentFormatType;

namespace Hmm.ServiceApi.Core.Tests
{
    public class NoteCatalogControllerTests : CoreTestFixtureBase
    {
        private readonly NoteCatalogManager _catalogManager;
        private readonly NoteCatalogController _controller;

        public NoteCatalogControllerTests()
        {
            _catalogManager = new NoteCatalogManager(CatalogRepository, Mapper, LookupRepository);
            _controller = new NoteCatalogController(_catalogManager, ApiMapper, new Mock<ILogger<NoteCatalogController>>().Object);
        }

        #region Get catalog by Id

        [Fact]
        public async Task Get_ReturnsOkResult_WithListOfCatalogs()
        {
            // Arrange
            // Act
            var result = await _controller.Get(new ResourceCollectionParameters());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnCatalogs = Assert.IsType<PageList<NoteCatalog>>(okResult.Value);
            Assert.Equal(4, returnCatalogs.Count);
        }

        [Fact]
        public async Task Get_ReturnsNotFound_WhenNoCatalogFound()
        {
            // Arrange
            ResetDataSource(ElementType.NoteCatalog);

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
        public async Task GetCatalogById_ReturnsOkResult_WithCatalog(int catalogId)
        {
            // Arrange
            // Act
            var result = await _controller.Get(catalogId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnCatalog = Assert.IsType<NoteCatalog>(okResult.Value);
            Assert.Equal(catalogId, returnCatalog.Id);
        }

        [Fact]
        public async Task GetCatalogById_ReturnsNotFound_WhenCatalogNotFound()
        {
            // Arrange
            const int catalogId = 20;
            // Act
            var result = await _controller.Get(catalogId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"The note catalog: {catalogId} not found.", notFoundResult.Value);
        }

        #endregion Get catalog by Id

        #region Add a new catalog

        [Fact]
        public async Task AddCatalog_ReturnsCreatedResult_WithNewCatalog()
        {
            // Arrange
            var apiCatalog = new ApiNoteCatalogForCreate
            {
                Name = "TodoList",
                FormatType = NoteContentFormatType.PlainText,
                Description = "Testing system note log catalog",
                Schema = "",
                IsDefault = false,
            };

            // Act
            var result = await _controller.Post(apiCatalog);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            var returnCatalog = Assert.IsType<NoteCatalog>(createdResult.Value);
            Assert.Equal(5, returnCatalog.Id);
        }

        [Fact]
        public async Task AddCatalog_ReturnsBadRequest_WhenCreationFails()
        {
            // Arrange
            var apiCatalog = new ApiNoteCatalogForCreate
            {
                Name = "Diary",
                FormatType = NoteContentFormatType.PlainText,
                Description = "Testing system note log catalog",
                Schema = "",
                IsDefault = false,
            };

            // Act
            var result = await _controller.Post(apiCatalog);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task AddCatalog_ReturnsInternalServerError_OnException()
        {
            // Arrange
            var apiCatalog = new ApiNoteCatalogForCreate
            {
                Name = "Diary",
                FormatType = NoteContentFormatType.PlainText,
                Description = "Testing system note log catalog",
                Schema = "",
                IsDefault = false,
            };
            var mockCatalogManager = new Mock<INoteCatalogManager>();
            mockCatalogManager.Setup(m => m.CreateAsync(It.IsAny<NoteCatalog>())).Throws(new Exception());
            var controller = new NoteCatalogController(mockCatalogManager.Object, ApiMapper, new Mock<ILogger<NoteCatalogController>>().Object);

            // Act
            var result = await controller.Post(apiCatalog);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion Add a new catalog

        #region Update catalog

        [Fact]
        public async Task UpdateCatalog_ReturnsNoContent_WhenUpdateIsSuccessful()
        {
            // Arrange
            const int catalogId = 100;
            var existingCatalogResult = await _catalogManager.GetEntityByIdAsync(catalogId);
            Assert.True(existingCatalogResult.Success);
            var existingCatalog = existingCatalogResult.Value;
            Assert.NotNull(existingCatalog);
            var apiCatalogForUpdate = new ApiNoteCatalogForUpdate
            {
                Name = "Diary",
                FormatType = NoteContentFormatType.Markdown,
                Description = "Testing note catalog",
                Schema = "",
                IsDefault = false
            };

            // Act
            var result = await _controller.Put(catalogId, apiCatalogForUpdate);
            var updatedCatalogResult = await _catalogManager.GetEntityByIdAsync(catalogId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.True(updatedCatalogResult.Success);
            Assert.Equal(catalogId, updatedCatalogResult.Value.Id);
            Assert.Equal(apiCatalogForUpdate.Name, updatedCatalogResult.Value.Name);
        }

        [Fact]
        public async Task UpdateCatalog_ReturnsBadRequest_WhenCatalogIsNullOrInvalidId()
        {
            // Arrange
            ApiNoteCatalogForUpdate? apiCatalogForUpdate = null;

            // Act
            var result = await _controller.Put(0, apiCatalogForUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Note catalog information is null or invalid id found", (badRequestResult.Value as ApiBadRequestResponse)?.Errors.FirstOrDefault());
            Assert.Equal("Bad request data", (badRequestResult.Value as ApiBadRequestResponse)?.Message);
        }

        [Fact]
        public async Task UpdateCatalog_ReturnsBadRequest_WhenCatalogNotFound()
        {
            // Arrange
            const int catalogId = 1000;
            var apiCatalogForUpdate = new ApiNoteCatalogForUpdate
            {
                Name = "UpdatedCatalog",
                FormatType = NoteContentFormatType.Markdown,
                Description = "Testing note catalog",
                Schema = "",
                IsDefault = false
            };

            // Act
            var result = await _controller.Put(catalogId, apiCatalogForUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal($"Note catalog {catalogId} cannot be found.", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateCatalog_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            var existingCatalogResult = await _catalogManager.GetEntityByIdAsync(100);
            Assert.True(existingCatalogResult.Success);
            var existingCatalog = existingCatalogResult.Value;

            var existingCatalog2Result = await _catalogManager.GetEntityByIdAsync(101);
            Assert.True(existingCatalog2Result.Success);
            var existingCatalog2 = existingCatalog2Result.Value;
            
            var apiCatalogForUpdate = new ApiNoteCatalogForUpdate
            {
                Name = existingCatalog2.Name,
                FormatType = NoteContentFormatType.Xml,
                Description = "Testing gas log catalog",
                Schema = "",
                IsDefault = false
            };

            // Act
            var result = await _controller.Put(existingCatalog.Id, apiCatalogForUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResponse = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.NotEmpty(apiResponse.Errors);
        }

        [Fact]
        public async Task UpdateCatalog_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int catalogId = 100;
            var apiCatalogForUpdate = new ApiNoteCatalogForUpdate { Name = "UpdatedCatalog" };
            var mockCatalogManager = new Mock<INoteCatalogManager>();
            mockCatalogManager.Setup(c => c.GetEntityByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => ProcessingResult<NoteCatalog>.Ok(new NoteCatalog { Id = id, Name = "Exists Catalog" }));
            mockCatalogManager.Setup(m => m.UpdateAsync(It.IsAny<NoteCatalog>())).Throws(new Exception());
            var controller = new NoteCatalogController(mockCatalogManager.Object, ApiMapper, new Mock<ILogger<NoteCatalogController>>().Object);

            // Act
            var result = await controller.Put(catalogId, apiCatalogForUpdate);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion Update catalog

        #region Patch catalog

        [Fact]
        public async Task PatchCatalog_ReturnsNoContent_WhenPatchIsSuccessful()
        {
            // Arrange
            const int catalogId = 100;
            var patchDoc = new JsonPatchDocument<ApiNoteCatalogForUpdate>();
            var existingCatalogResult = await _catalogManager.GetEntityByIdAsync(catalogId);
            Assert.True(existingCatalogResult.Success);
            var existingCatalog = existingCatalogResult.Value;
            Assert.NotNull(existingCatalog);
            
            patchDoc.Replace(e => e.Description, "Updated note catalog with new description");

            // Act
            var result = await _controller.Patch(catalogId, patchDoc);
            var updatedCatalogResult = await _catalogManager.GetEntityByIdAsync(catalogId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.True(updatedCatalogResult.Success);
            Assert.Equal("Updated note catalog with new description", updatedCatalogResult.Value.Description);
        }

        [Fact]
        public async Task PatchCatalog_ReturnsBadRequest_WhenPatchDocIsNullOrInvalidId()
        {
            // Arrange
            JsonPatchDocument<ApiNoteCatalogForUpdate>? patchDoc = null;

            // Act
            var result = await _controller.Patch(0, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Patch information is null or invalid id found", (badRequestResult.Value as ApiBadRequestResponse)?.Errors.FirstOrDefault());
            Assert.Equal("Bad request data", (badRequestResult.Value as ApiBadRequestResponse)?.Message);
        }

        [Fact]
        public async Task PatchCatalog_ReturnsNotFound_WhenCatalogNotFound()
        {
            // Arrange
            const int catalogId = 1;
            var patchDoc = new JsonPatchDocument<ApiNoteCatalogForUpdate>();

            // Act
            var result = await _controller.Patch(catalogId, patchDoc);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task PatchCatalog_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            const int catalogId = 100;
            var patchDoc = new JsonPatchDocument<ApiNoteCatalogForUpdate>();
            var existingCatalogResult = await _catalogManager.GetEntityByIdAsync(catalogId);
            Assert.True(existingCatalogResult.Success);
            var existingCatalog = existingCatalogResult.Value;
            patchDoc.Replace(e => e.Name, existingCatalog.Name);

            // Act
            var result = await _controller.Patch(catalogId + 1, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResponse = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.NotEmpty(apiResponse.Errors);
        }

        [Fact]
        public async Task PatchCatalog_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int catalogId = 100;
            var patchDoc = new JsonPatchDocument<ApiNoteCatalogForUpdate>();
            patchDoc.Replace(e => e.Name, "SomeNewName");

            var mockCatalogManager = new Mock<INoteCatalogManager>();
            mockCatalogManager.Setup(a => a.GetEntityByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => ProcessingResult<NoteCatalog>.Ok(new NoteCatalog { Id = id, Name = "Exists Catalog" }));
            mockCatalogManager.Setup(m => m.UpdateAsync(It.IsAny<NoteCatalog>())).Throws(new Exception());
            var controller = new NoteCatalogController(mockCatalogManager.Object, ApiMapper, new Mock<ILogger<NoteCatalogController>>().Object);

            // Act
            var result = await controller.Patch(catalogId, patchDoc);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion Patch catalog
    }
}