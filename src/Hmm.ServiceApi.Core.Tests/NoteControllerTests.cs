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
    public class NoteControllerTests : CoreTestFixtureBase
    {
        private readonly HmmNoteManager _noteManager;
        private readonly HmmNoteController _controller;
        private readonly NoteTagAssociationManager _noteTagAssociationManager;
        private readonly Mock<IHmmValidator<HmmNote>> _mockValidator;

        public NoteControllerTests()
        {
            _mockValidator = new Mock<IHmmValidator<HmmNote>>();
            _mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<HmmNote>()))
                .ReturnsAsync(ProcessingResult<HmmNote>.Ok(It.IsAny<HmmNote>()));
            var tagValidator = new Mock<IHmmValidator<Tag>>();
            tagValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<Tag>()))
                .ReturnsAsync(ProcessingResult<Tag>.Ok(It.IsAny<Tag>()));
            _noteManager = new HmmNoteManager(NoteRepository, UnitOfWork, Mapper, LookupRepository, DateProvider, _mockValidator.Object);
            var tagManager = new TagManager(TagRepository, Mapper, LookupRepository, tagValidator.Object);
            _noteTagAssociationManager = new NoteTagAssociationManager(_noteManager, tagManager);
            _controller = new HmmNoteController(_noteManager, _noteTagAssociationManager, ApiMapper, new Mock<ILogger<HmmNoteController>>().Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        private HmmNoteController CreateControllerWithMockedManager(Mock<IHmmNoteManager> mockManager)
        {
            var controller = new HmmNoteController(mockManager.Object, _noteTagAssociationManager, ApiMapper, new Mock<ILogger<HmmNoteController>>().Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }

        #region Get note by Id

        [Fact]
        public async Task Get_ReturnsOkResult_WithListOfNotes()
        {
            // Arrange
            // Act
            var result = await _controller.Get(new ResourceCollectionParameters());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnNotes = Assert.IsType<PageList<HmmNote>>(okResult.Value);
            Assert.Equal(5, returnNotes.Count);
        }

        [Fact]
        public async Task Get_ReturnsNotFound_WhenNoNoteFound()
        {
            // Arrange
            ResetDataSource(ElementType.Note);
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
        public async Task GetNoteById_ReturnsOkResult_WithNote(int noteId)
        {
            // Arrange
            // Act
            var result = await _controller.Get(noteId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnNote = Assert.IsType<HmmNote>(okResult.Value);
            Assert.Equal(noteId, returnNote.Id);
        }

        [Fact]
        public async Task GetNoteById_ReturnsNotFound_WhenNoteNotFound()
        {
            // Arrange
            const int noteId = 20;
            // Act
            var result = await _controller.Get(noteId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"The note {noteId} not found.", notFoundResult.Value);
        }

        #endregion Get note by Id

        #region Add a new note

        [Fact]
        public async Task AddNote_ReturnsCreatedResult_WithNewNote()
        {
            // Arrange
            var note = SampleDataGenerator.GetNote();
            note.Author = await GetTestAuthor();
            note.Catalog = await GetTestCatalog();
            var apiNote = ApiMapper.Map<ApiNoteForCreate>(note);
            apiNote.Subject = "Test Note";

            // Act
            var result = await _controller.Post(apiNote);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            var returnNote = Assert.IsType<HmmNote>(createdResult.Value);
            Assert.Equal(6, returnNote.Id);
        }

        [Fact]
        public async Task AddNote_ReturnsBadRequest_WhenCreationFails()
        {
            // Arrange
            var apiNote = ApiMapper.Map<ApiNoteForCreate>(SampleDataGenerator.GetNote());
            apiNote.Subject = GetRandomString(2000);

            // Set up validator to return validation error for subject too long
            _mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<HmmNote>()))
                .ReturnsAsync(ProcessingResult<HmmNote>.Invalid("Subject is too long"));

            // Act
            var result = await _controller.Post(apiNote);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddNote_ReturnsInternalServerError_OnException()
        {
            // Arrange
            var apiNote = new ApiNoteForCreate { Subject = "TestNote" };
            var mockNoteManager = new Mock<IHmmNoteManager>();
            mockNoteManager.Setup(m => m.CreateAsync(It.IsAny<HmmNote>())).Throws(new Exception());
            var controller = CreateControllerWithMockedManager(mockNoteManager);

            // Act
            var result = await controller.Post(apiNote);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        }

        #endregion Add a new note

        #region Update note

        [Fact]
        public async Task UpdateNote_ReturnsNoContent_WhenUpdateIsSuccessful()
        {
            // Arrange
            const int noteId = 100;
            var existingNoteResult = await _noteManager.GetNoteByIdAsync(noteId);
            var existingNote = existingNoteResult.Value;
            Assert.NotNull(existingNote);
            var apiNoteForUpdate = ApiMapper.Map<ApiNoteForUpdate>(existingNote);
            apiNoteForUpdate.Subject = "Updated note subject";

            // Act
            var result = await _controller.Put(noteId, apiNoteForUpdate);
            var updatedNoteResult = await _noteManager.GetNoteByIdAsync(noteId);
            var updatedNote = updatedNoteResult.Value;

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(noteId, updatedNote.Id);
            Assert.Equal(apiNoteForUpdate.Subject, updatedNote.Subject);
        }

        [Fact]
        public async Task UpdateNote_ReturnsBadRequest_WhenNoteIsNullOrInvalidId()
        {
            // Arrange
            ApiNoteForUpdate? apiNoteForUpdate = null;

            // Act
            var result = await _controller.Put(0, apiNoteForUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Note information is null or invalid id found", (badRequestResult.Value as ApiBadRequestResponse)?.Errors.FirstOrDefault());
            Assert.Equal("Bad request data", (badRequestResult.Value as ApiBadRequestResponse)?.Message);
        }

        [Fact]
        public async Task UpdateNote_ReturnsNotFound_WhenNoteNotFound()
        {
            // Arrange
            const int noteId = 1000;
            var existsNoteResult = await _noteManager.GetNoteByIdAsync(100);
            var existsNote = existsNoteResult.Value;
            var apiNoteForUpdate = ApiMapper.Map<ApiNoteForUpdate>(existsNote);
            apiNoteForUpdate.Subject = "Updated note subject";

            // Act
            var result = await _controller.Put(noteId, apiNoteForUpdate);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"Note with id {noteId} not found", notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateNote_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            var existingNoteResult = await _noteManager.GetNoteByIdAsync(100);
            var existingNote = existingNoteResult.Value;
            var apiNoteForUpdate = ApiMapper.Map<ApiNoteForUpdate>(existingNote);
            apiNoteForUpdate.Subject = GetRandomString(2000);

            // Set up validator to return validation error for subject too long
            _mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<HmmNote>()))
                .ReturnsAsync(ProcessingResult<HmmNote>.Invalid("Subject is too long"));

            // Act
            var result = await _controller.Put(existingNote.Id, apiNoteForUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResponse = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.NotEmpty(apiResponse.Errors);
        }

        [Fact]
        public async Task UpdateNote_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int noteId = 100;
            var apiNoteForUpdate = new ApiNoteForUpdate { Subject = "UpdatedNote" };
            var mockNoteManager = new Mock<IHmmNoteManager>();
            var note = new HmmNote { Id = noteId, Subject = "Exists Note" };
            mockNoteManager.Setup(a => a.GetNoteByIdAsync(It.IsAny<int>(), false)).ReturnsAsync(ProcessingResult<HmmNote>.Ok(note));
            mockNoteManager.Setup(m => m.UpdateAsync(It.IsAny<HmmNote>())).Throws(new Exception());
            var controller = CreateControllerWithMockedManager(mockNoteManager);

            // Act
            var result = await controller.Put(noteId, apiNoteForUpdate);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        }

        #endregion Update note

        #region Apply tag to note

        [Fact]
        public async Task ApplyTag_ReturnsNoContent_WhenTagAppliedSuccessfully()
        {
            // Arrange
            const int noteId = 100;
            var tagDto = new ApiTagForApply { Id = 100, Name = "Important" };
            var noteResult = await _noteManager.GetNoteByIdAsync(noteId);
            var note = noteResult.Value;
            Assert.NotNull(note);
            Assert.Empty(note.Tags);

            // Act
            var result = await _controller.ApplyTag(noteId, tagDto);
            noteResult = await _noteManager.GetNoteByIdAsync(noteId);
            note = noteResult.Value;

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Single(note.Tags);
        }

        [Fact]
        public async Task ApplyTag_ReturnsBadRequest_WhenTagIsNull()
        {
            // Arrange
            const int noteId = 100;
            ApiTagForApply? tagDto = null;

            // Act
            var result = await _controller.ApplyTag(noteId, tagDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task ApplyTag_ReturnsBadRequest_WhenTagIsEmpty()
        {
            // Arrange
            const int noteId = 100;
            var tagDto = new ApiTagForApply { Name = "" };

            // Act
            var result = await _controller.ApplyTag(noteId, tagDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task ApplyTag_ReturnsServerError_WhenNoteDoesNotExist()
        {
            // Arrange
            const int noteId = 10000;
            var tagDto = new ApiTagForApply { Name = "Important" };

            // Act
            var result = await _controller.ApplyTag(noteId, tagDto);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        }

        [Fact]
        public async Task ApplyTag_ReturnsBadRequest_WhenTagApplicationFails()
        {
            // Arrange
            const int noteId = 100;
            var tagDto = new ApiTagForApply { Name = "Important" };
            var note = new HmmNote { Id = noteId, Tags = [] };
            var tag = new Tag { Name = "Important" };

            var noteManagerMock = new Mock<IHmmNoteManager>();
            noteManagerMock.Setup(m => m.GetNoteByIdAsync(noteId, false)).ReturnsAsync(ProcessingResult<HmmNote>.Ok(note));

            var noteTagAssociationManagerMock = new Mock<INoteTagAssociationManager>();
            noteTagAssociationManagerMock.Setup(m => m.ApplyTagToNoteAsync(noteId, It.IsAny<Tag>())).ReturnsAsync(ProcessingResult<List<Tag>>.Invalid("something went wrong"));

            var controller =  new HmmNoteController(noteManagerMock.Object, noteTagAssociationManagerMock.Object, ApiMapper, new Mock<ILogger<HmmNoteController>>().Object);

            // Act
            var result = await controller.ApplyTag(noteId, tagDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        #endregion Apply tag to note

        #region Patch note

        [Fact]
        public async Task PatchNote_ReturnsNoContent_WhenPatchIsSuccessful()
        {
            // Arrange
            const int noteId = 100;
            var patchDoc = new JsonPatchDocument<ApiNoteForUpdate>();
            var existingNoteResult = await _noteManager.GetNoteByIdAsync(noteId);
            var existingNote = existingNoteResult.Value;
            Assert.NotNull(existingNote);
            patchDoc.Replace(e => e.Description, "UpdatedNote with new description");

            // Act
            var result = await _controller.Patch(noteId, patchDoc);
            var updatedNoteResult = await _noteManager.GetNoteByIdAsync(noteId);
            var updatedNote = updatedNoteResult.Value;

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal("UpdatedNote with new description", updatedNote.Description);
        }

        [Fact]
        public async Task PatchNote_ReturnsBadRequest_WhenPatchDocIsNullOrInvalidId()
        {
            // Arrange
            JsonPatchDocument<ApiNoteForUpdate>? patchDoc = null;

            // Act
            var result = await _controller.Patch(0, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Patch information is null or invalid id found", (badRequestResult.Value as ApiBadRequestResponse)?.Errors.FirstOrDefault());
            Assert.Equal("Bad request data", (badRequestResult.Value as ApiBadRequestResponse)?.Message);
        }

        [Fact]
        public async Task PatchNote_ReturnsNotFound_WhenNoteNotFound()
        {
            // Arrange
            const int noteId = 1;
            var patchDoc = new JsonPatchDocument<ApiNoteForUpdate>();

            // Act
            var result = await _controller.Patch(noteId, patchDoc);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"Note with id {noteId} not found", notFoundResult.Value);
        }

        [Fact]
        public async Task PatchNote_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            const int noteId = 100;
            var patchDoc = new JsonPatchDocument<ApiNoteForUpdate>();
            patchDoc.Replace(e => e.Subject, GetRandomString(2000));

            // Set up validator to return validation error for subject too long
            _mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<HmmNote>()))
                .ReturnsAsync(ProcessingResult<HmmNote>.Invalid("Subject is too long"));

            // Act
            var result = await _controller.Patch(noteId, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResponse = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.NotEmpty(apiResponse.Errors);
        }

        [Fact]
        public async Task PatchNote_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int noteId = 100;
            var patchDoc = new JsonPatchDocument<ApiNoteForUpdate>();
            patchDoc.Replace(e => e.Subject, "SomeNewSubject");

            var note = new HmmNote { Id = noteId, Subject = "Exists Note" };
            var mockNoteManager = new Mock<IHmmNoteManager>();
            mockNoteManager.Setup(a => a.GetNoteByIdAsync(It.IsAny<int>(), false)).ReturnsAsync(ProcessingResult<HmmNote>.Ok(note));
            mockNoteManager.Setup(m => m.UpdateAsync(It.IsAny<HmmNote>())).Throws(new Exception());
            var controller = CreateControllerWithMockedManager(mockNoteManager);

            // Act
            var result = await controller.Patch(noteId, patchDoc);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        }

        #endregion Patch note

        #region Delete note

        [Fact]
        public async Task Delete_ReturnsNoContent_WhenDeleteIsSuccessful()
        {
            // Arrange
            const int noteId = 100;
            var existingNoteResult = await _noteManager.GetNoteByIdAsync(noteId);
            Assert.True(existingNoteResult.Success);
            Assert.NotNull(existingNoteResult.Value);

            // Act
            var result = await _controller.Delete(noteId);
            var deletedNoteResult = await _noteManager.GetNoteByIdAsync(noteId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.False(deletedNoteResult.Success);
            Assert.Equal(ErrorCategory.Deleted, deletedNoteResult.ErrorType);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenIdIsInvalid()
        {
            // Act
            var result = await _controller.Delete(0);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Note with id 0 not found", notFoundResult.Value);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenNoteNotFound()
        {
            // Arrange
            const int noteId = 1;

            // Act
            var result = await _controller.Delete(noteId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"Note with id {noteId} not found", notFoundResult.Value);
        }

        [Fact]
        public async Task Delete_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int noteId = 1;
            var note = new HmmNote { Id = noteId, Subject = "Exists Note" };
            var mockNoteManager = new Mock<IHmmNoteManager>();
            mockNoteManager.Setup(a => a.GetNoteByIdAsync(It.IsAny<int>(), false)).ReturnsAsync(ProcessingResult<HmmNote>.Ok(note));
            mockNoteManager.Setup(m => m.UpdateAsync(It.IsAny<HmmNote>())).Throws(new Exception());
            var controller = CreateControllerWithMockedManager(mockNoteManager);

            // Act
            var result = await controller.Delete(noteId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        }

        #endregion Delete note
    }
}