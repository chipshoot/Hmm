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
    public class NoteControllerTests : CoreTestFixtureBase
    {
        private readonly HmmNoteManager _noteManager;
        private readonly HmmNoteController _controller;

        public NoteControllerTests()
        {
            var tagManager = new TagManager(TagRepository, Mapper, LookupRepository);
            _noteManager = new HmmNoteManager(NoteRepository, Mapper, tagManager, LookupRepository, DateProvider);
            _controller = new HmmNoteController(_noteManager, ApiMapper);
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
            var controller = new HmmNoteController(mockNoteManager.Object, ApiMapper);

            // Act
            var result = await controller.Post(apiNote);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion Add a new note

        #region Update note

        [Fact]
        public async Task UpdateNote_ReturnsNoContent_WhenUpdateIsSuccessful()
        {
            // Arrange
            const int noteId = 100;
            var existingNote = await _noteManager.GetNoteByIdAsync(noteId);
            Assert.NotNull(existingNote);
            var apiNoteForUpdate = ApiMapper.Map<ApiNoteForUpdate>(existingNote);
            apiNoteForUpdate.Subject = "Updated note subject";

            // Act
            var result = await _controller.Put(noteId, apiNoteForUpdate);
            var updatedNote = await _noteManager.GetNoteByIdAsync(noteId);

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
        public async Task UpdateNote_ReturnsBadRequest_WhenNoteNotFound()
        {
            // Arrange
            const int noteId = 1000;
            var existsNote = await _noteManager.GetNoteByIdAsync(100);
            var apiNoteForUpdate = ApiMapper.Map<ApiNoteForUpdate>(existsNote);
            apiNoteForUpdate.Subject = "Updated note subject";

            // Act
            var result = await _controller.Put(noteId, apiNoteForUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal($"The note {noteId} cannot be found.", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateNote_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            var existingNote = await _noteManager.GetNoteByIdAsync(100);
            var apiNoteForUpdate = ApiMapper.Map<ApiNoteForUpdate>(existingNote);
            apiNoteForUpdate.Subject = GetRandomString(2000);

            // Act
            var result = await _controller.Put(existingNote.Id, apiNoteForUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(_noteManager.ProcessResult.MessageList, badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateNote_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int noteId = 100;
            var apiNoteForUpdate = new ApiNoteForUpdate { Subject = "UpdatedNote" };
            var mockNoteManager = new Mock<IHmmNoteManager>();
            var note = new HmmNote { Id = noteId, Subject = "Exists Note" };
            mockNoteManager.Setup(a => a.GetNoteByIdAsync(It.IsAny<int>(), false)).ReturnsAsync(note);
            mockNoteManager.Setup(m => m.UpdateAsync(It.IsAny<HmmNote>())).Throws(new Exception());
            var controller = new HmmNoteController(mockNoteManager.Object, ApiMapper);

            // Act
            var result = await controller.Put(noteId, apiNoteForUpdate);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion Update note

        #region Apply tag to note

        [Fact]
        public async Task ApplyTag_ReturnsNoContent_WhenTagAppliedSuccessfully()
        {
            // Arrange
            const int noteId = 100;
            var tagDto = new ApiTagForApply { Id = 100, Name = "Important" };
            var note = await _noteManager.GetNoteByIdAsync(noteId);
            Assert.NotNull(note);
            Assert.Empty(note.Tags);

            // Act
            var result = await _controller.ApplyTag(noteId, tagDto);
            note = await _noteManager.GetNoteByIdAsync(noteId);

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
        public async Task ApplyTag_ReturnsNotFound_WhenNoteDoesNotExist()
        {
            // Arrange
            const int noteId = 10000;
            var tagDto = new ApiTagForApply { Name = "Important" };

            // Act
            var result = await _controller.ApplyTag(noteId, tagDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task ApplyTag_ReturnsBadRequest_WhenTagApplicationFails()
        {
            // Arrange
            const int noteId = 100;
            var tagDto = new ApiTagForApply { Name = "Important" };
            var note = new HmmNote { Id = noteId, Tags = [] };
            var tag = new Tag { Name = "Important" };

            var errorMsg = new ProcessingResult();
            errorMsg.AddErrorMessage("something went wrong");
            var noteManagerMock = new Mock<IHmmNoteManager>();
            noteManagerMock.Setup(m => m.GetNoteByIdAsync(noteId, false)).ReturnsAsync(note);
            noteManagerMock.Setup(m => m.ApplyTag(note, tag)).ReturnsAsync(null as List<Tag>);
            noteManagerMock.Setup(m => m.ProcessResult).Returns(errorMsg);
            var controller =  new HmmNoteController(noteManagerMock.Object, ApiMapper);

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
            var existingNote = await _noteManager.GetNoteByIdAsync(noteId);
            Assert.NotNull(existingNote);
            patchDoc.Replace(e => e.Description, "UpdatedNote with new description");

            // Act
            var result = await _controller.Patch(noteId, patchDoc);
            var updatedNote = await _noteManager.GetNoteByIdAsync(noteId);

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
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task PatchNote_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            const int noteId = 100;
            var patchDoc = new JsonPatchDocument<ApiNoteForUpdate>();
            patchDoc.Replace(e => e.Subject, GetRandomString(2000));

            // Act
            var result = await _controller.Patch(noteId, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(_noteManager.ProcessResult.MessageList, badRequestResult.Value);
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
            mockNoteManager.Setup(a => a.GetNoteByIdAsync(It.IsAny<int>(), false)).ReturnsAsync(note);
            mockNoteManager.Setup(m => m.UpdateAsync(It.IsAny<HmmNote>())).Throws(new Exception());
            var controller = new HmmNoteController(mockNoteManager.Object, ApiMapper);

            // Act
            var result = await controller.Patch(noteId, patchDoc);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion Patch note

        #region Delete note

        [Fact]
        public async Task Delete_ReturnsNoContent_WhenDeleteIsSuccessful()
        {
            // Arrange
            const int noteId = 100;
            var existingNote = await _noteManager.GetNoteByIdAsync(noteId);
            Assert.NotNull(existingNote);

            // Act
            var result = await _controller.Delete(noteId);
            var deletedNote = await _noteManager.GetNoteByIdAsync(noteId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(deletedNote);
        }

        [Fact]
        public async Task Delete_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Act
            var result = await _controller.Delete(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid note id found", (badRequestResult.Value as ApiBadRequestResponse)?.Errors.FirstOrDefault());
        }

        [Fact]
        public async Task Delete_ReturnsBadRequest_WhenNoteNotFound()
        {
            // Arrange
            const int noteId = 1;

            // Act
            var result = await _controller.Delete(noteId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal($"The note {noteId} cannot be found.", badRequestResult.Value);
        }

        [Fact]
        public async Task Delete_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int noteId = 1;
            var note = new HmmNote { Id = noteId, Subject = "Exists Note" };
            var mockNoteManager = new Mock<IHmmNoteManager>();
            mockNoteManager.Setup(a => a.GetNoteByIdAsync(It.IsAny<int>(), false)).ReturnsAsync(note);
            mockNoteManager.Setup(m => m.DeleteAsync(It.IsAny<int>())).Throws(new Exception());
            var controller = new HmmNoteController(mockNoteManager.Object, ApiMapper);

            // Act
            var result = await controller.Delete(noteId);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion Delete note
    }
}