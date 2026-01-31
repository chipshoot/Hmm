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
using AddressType = Hmm.ServiceApi.DtoEntity.HmmNote.AddressType;
using EmailType = Hmm.ServiceApi.DtoEntity.HmmNote.EmailType;
using TelephoneType = Hmm.ServiceApi.DtoEntity.HmmNote.TelephoneType;

namespace Hmm.ServiceApi.Core.Tests
{
    public class ContactControllerTests : CoreTestFixtureBase
    {
        private readonly ContactManager _contactManager;
        private readonly ContactController _controller;
        private readonly Mock<IHmmValidator<Contact>> _mockValidator;

        public ContactControllerTests()
        {
            _mockValidator = new Mock<IHmmValidator<Contact>>();
            _mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<Contact>()))
                .ReturnsAsync(ProcessingResult<Contact>.Ok(It.IsAny<Contact>()));
            _contactManager = new ContactManager(ContactRepository, Mapper, LookupRepository, _mockValidator.Object);
            _controller = new ContactController(_contactManager, ApiMapper, new Mock<ILogger<ContactController>>().Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        private ContactController CreateControllerWithMockedManager(Mock<IContactManager> mockManager)
        {
            var controller = new ContactController(mockManager.Object, ApiMapper, new Mock<ILogger<ContactController>>().Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }

        #region Get contact by Id

        [Fact]
        public async Task Get_ReturnsOkResult_WithListOfContacts()
        {
            // Arrange
            // Act
            var result = await _controller.Get(new ResourceCollectionParameters());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnContacts = Assert.IsType<PageList<Contact>>(okResult.Value);
            Assert.Equal(4, returnContacts.Count);
        }

        [Fact]
        public async Task Get_ReturnsNotFound_WhenNoContactFound()
        {
            // Arrange
            ResetDataSource(ElementType.Contact);
            // Act
            var result = await _controller.Get(new ResourceCollectionParameters());

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal("No contacts found.", problemDetails.Detail);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(101)]
        [InlineData(102)]
        [InlineData(103)]
        public async Task GetContactById_ReturnsOkResult_WithContact(int contactId)
        {
            // Arrange
            // Act
            var result = await _controller.Get(contactId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnContact = Assert.IsType<Contact>(okResult.Value);
            Assert.Equal(contactId, returnContact.Id);
        }

        [Fact]
        public async Task GetContactById_ReturnsNotFound_WhenContactNotFound()
        {
            // Arrange
            const int contactId = 20;
            // Act
            var result = await _controller.Get(contactId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal($"The contact : {contactId} not found.", problemDetails.Detail);
        }

        #endregion Get contact by Id

        #region Add a new contact

        [Fact]
        public async Task AddContact_ReturnsCreatedResult_WithNewContact()
        {
            // Arrange
            var apiContact = new ApiContactForCreate
            {
                FirstName = "Jack",
                LastName = "Fang",
                Emails = new List<ApiEmail>
                {
                    new() { Address = "john.doe@example.com", IsPrimary = true, Type = EmailType.Work},
                    new() { Address = "john.doe@work.com", IsPrimary = false, Type = EmailType.Personal}
                },
                Phones = new List<ApiPhone>
                {
                    new() { Number = "123-456-7890", Type = TelephoneType.Home },
                    new() { Number = "098-765-4321", Type = TelephoneType.Mobile }
                },
                Addresses = new List<ApiAddressInfo>
                {
                    new() { Address = "123 Main St", City = "Toronto", State = "CA", Country = "Canada", PostalCode = "12345", Type = AddressType.Home}
                },
                IsActivated = true
            };

            // Act
            var result = await _controller.Post(apiContact);

            // Assert
            var createdResult = Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Equal("GetContactById", createdResult.RouteName);
            Assert.Equal("1.0", createdResult.RouteValues?["version"]);
            var returnContact = Assert.IsType<Contact>(createdResult.Value);
            Assert.Equal(5, returnContact.Id);
            Assert.Equal(returnContact.Id, createdResult.RouteValues?["id"]);
        }

        [Fact]
        public async Task AddContact_ReturnsBadRequest_WhenCreationFails()
        {
            // Arrange
            var apiContact = new ApiContactForCreate
            {
                FirstName = GetRandomString(250),
                LastName = "Fang",
                Emails = new List<ApiEmail>
                {
                    new() { Address = "john.doe@example.com", IsPrimary = true, Type = EmailType.Work},
                    new() { Address = "john.doe@work.com", IsPrimary = false, Type = EmailType.Personal}
                },
                Phones = new List<ApiPhone>
                {
                    new() { Number = "123-456-7890", Type = TelephoneType.Home },
                    new() { Number = "098-765-4321", Type = TelephoneType.Mobile }
                },
                Addresses = new List<ApiAddressInfo>
                {
                    new() { Address = "123 Main St", City = "Toronto", State = "ON", Country = "Canada", PostalCode = "12345", Type = AddressType.Home}
                },
                IsActivated = true
            };

            // Set up validator to return validation error for first name too long
            _mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<Contact>()))
                .ReturnsAsync(ProcessingResult<Contact>.Invalid("First name is too long"));

            // Act
            var result = await _controller.Post(apiContact);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddContact_ReturnsInternalServerError_OnException()
        {
            // Arrange
            var apiContact = new ApiContactForCreate { FirstName = "TestContact" };
            var mockContactManager = new Mock<IContactManager>();
            mockContactManager.Setup(m => m.CreateAsync(It.IsAny<Contact>())).Throws(new Exception());
            var controller = CreateControllerWithMockedManager(mockContactManager);

            // Act
            var result = await controller.Post(apiContact);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("An unexpected error occurred while creating the contact.", problemDetails.Detail);
        }

        #endregion Add a new contact

        #region Update contact

        [Fact]
        public async Task UpdateContact_ReturnsNoContent_WhenUpdateIsSuccessful()
        {
            // Arrange
            const int contactId = 100;
            var existingContactResult = await _contactManager.GetContactByIdAsync(contactId);
            Assert.True(existingContactResult.Success);
            var existingContact = existingContactResult.Value;
            Assert.NotNull(existingContact);
            var apiContactForUpdate = ApiMapper.Map<ApiContactForUpdate>(existingContact);
            apiContactForUpdate.Description = "Updated contact description";

            // Act
            var result = await _controller.Put(contactId, apiContactForUpdate);
            var updatedContactResult = await _contactManager.GetContactByIdAsync(contactId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.True(updatedContactResult.Success);
            Assert.Equal(contactId, updatedContactResult.Value.Id);
            Assert.Equal(apiContactForUpdate.Description, updatedContactResult.Value.Description);
        }

        [Fact]
        public async Task UpdateContact_ReturnsBadRequest_WhenContactIsNullOrInvalidId()
        {
            // Arrange
            ApiContactForUpdate? apiContactForUpdate = null;

            // Act
            var result = await _controller.Put(0, apiContactForUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Equal("Contact information is null or invalid id found", problemDetails.Detail);
        }

        [Fact]
        public async Task UpdateContact_ReturnsNotFound_WhenContactNotFound()
        {
            // Arrange
            const int contactId = 1000;
            var apiContactForUpdate = new ApiContactForUpdate
            {
                FirstName = "Jane",
                LastName = "Smith",
                Emails = new List<ApiEmail>
                {
                    new() { Address = "jane.smith@example.com", IsPrimary = true, Type = EmailType.Other }
                },
                Phones = new List<ApiPhone>
                {
                    new() { Number = "555-555-5555", Type = TelephoneType.Work, IsPrimary = true }
                },
                Addresses = new List<ApiAddressInfo>
                {
                    new()
                    {
                        Address = "456 Elm St", City = "Toronto", State = "NY", Country = "Canada", PostalCode = "67890",
                        Type = AddressType.Home
                    }
                },
                IsActivated = false
            };

            // Act
            var result = await _controller.Put(contactId, apiContactForUpdate);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal($"Contact with id {contactId} not found", problemDetails.Detail);
        }

        [Fact]
        public async Task UpdateContact_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            var existingContactResult = await _contactManager.GetContactByIdAsync(100);
            Assert.True(existingContactResult.Success);
            var existingContact = existingContactResult.Value;
            var apiContactForUpdate = ApiMapper.Map<ApiContactForUpdate>(existingContact);
            apiContactForUpdate.LastName = GetRandomString(255);

            // Set up validator to return validation error
            _mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<Contact>()))
                .ReturnsAsync(ProcessingResult<Contact>.Invalid("Last name is too long"));

            // Act
            var result = await _controller.Put(existingContact.Id, apiContactForUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Contains("Last name is too long", problemDetails.Detail);
        }

        [Fact]
        public async Task UpdateContact_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int contactId = 100;
            var existContactResult = await _contactManager.GetContactByIdAsync(contactId);
            Assert.True(existContactResult.Success);
            var existContact = existContactResult.Value;
            var apiContactForUpdate = ApiMapper.Map<ApiContactForUpdate>(existContact);
            var mockContactManager = new Mock<IContactManager>();
            mockContactManager.Setup(m => m.UpdateAsync(It.IsAny<Contact>())).Throws(new Exception());
            var controller = CreateControllerWithMockedManager(mockContactManager);

            // Act
            var result = await controller.Put(contactId, apiContactForUpdate);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("An unexpected error occurred while updating the contact.", problemDetails.Detail);
        }

        #endregion Update contact

        #region Patch contact

        [Fact]
        public async Task PatchContact_ReturnsNoContent_WhenPatchIsSuccessful()
        {
            // Arrange
            const int contactId = 100;
            var patchDoc = new JsonPatchDocument<ApiContactForUpdate>();
            var existsContactResult = await _contactManager.GetContactByIdAsync(contactId);
            Assert.True(existsContactResult.Success);
            var existsContact = existsContactResult.Value;
            Assert.NotNull(existsContact);
            patchDoc.Replace(c => c.Description, "Updated contact with new description");

            // Act
            var result = await _controller.Patch(contactId, patchDoc);
            var updatedContactResult = await _contactManager.GetContactByIdAsync(contactId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.True(updatedContactResult.Success);
            Assert.Equal("Updated contact with new description", updatedContactResult.Value.Description);
        }

        [Fact]
        public async Task PatchContact_ReturnsBadRequest_WhenPatchDocIsNullOrInvalidId()
        {
            // Arrange
            JsonPatchDocument<ApiContactForUpdate>? patchDoc = null;

            // Act
            var result = await _controller.Patch(0, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Equal("Patch information is null or invalid id found", problemDetails.Detail);
        }

        [Fact]
        public async Task PatchContact_ReturnsNotFound_WhenContactNotFound()
        {
            // Arrange
            const int contactId = 1;
            var patchDoc = new JsonPatchDocument<ApiContactForUpdate>();

            // Act
            var result = await _controller.Patch(contactId, patchDoc);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal($"Contact with id {contactId} not found", problemDetails.Detail);
        }

        [Fact]
        public async Task PatchContact_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            const int contactId = 100;
            var patchDoc = new JsonPatchDocument<ApiContactForUpdate>();
            patchDoc.Replace(e => e.LastName, GetRandomString(255));

            // Set up validator to return validation error
            _mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<Contact>()))
                .ReturnsAsync(ProcessingResult<Contact>.Invalid("Last name is too long"));

            // Act
            var result = await _controller.Patch(contactId, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Contains("Last name is too long", problemDetails.Detail);
        }

        [Fact]
        public async Task PatchContact_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int contactId = 100;
            var patchDoc = new JsonPatchDocument<ApiContactForUpdate>();
            patchDoc.Replace(e => e.FirstName, "SomeNewName");

            var mockContactManager = new Mock<IContactManager>();
            mockContactManager.Setup(a => a.GetContactByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => ProcessingResult<Contact>.Ok(new Contact { Id = id, LastName = "Exists Contact" }));
            mockContactManager.Setup(m => m.UpdateAsync(It.IsAny<Contact>())).Throws(new Exception());
            var controller = CreateControllerWithMockedManager(mockContactManager);

            // Act
            var result = await controller.Patch(contactId, patchDoc);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("An unexpected error occurred while patching the contact.", problemDetails.Detail);
        }

        #endregion Patch contact

        #region Delete contact

        [Fact]
        public async Task Delete_ReturnsNoContent_WhenDeleteIsSuccessful()
        {
            // Arrange
            const int contactId = 100;
            var existingContactResult = await _contactManager.GetContactByIdAsync(contactId);
            Assert.True(existingContactResult.Success);
            Assert.NotNull(existingContactResult.Value);

            // Act
            var result = await _controller.Delete(contactId);
            var deletedContactResult = await _contactManager.GetContactByIdAsync(contactId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.False(deletedContactResult.Success);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenIdIsInvalid()
        {
            // Act
            var result = await _controller.Delete(0);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal("Contact with id 0 not found", problemDetails.Detail);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenContactNotFound()
        {
            // Arrange
            const int contactId = 1;

            // Act
            var result = await _controller.Delete(contactId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal($"Contact with id {contactId} not found", problemDetails.Detail);
        }

        [Fact]
        public async Task Delete_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int contactId = 1;
            var mockContactManager = new Mock<IContactManager>();
            mockContactManager.Setup(m => m.DeActivateAsync(It.IsAny<int>())).Throws(new Exception());
            var controller = CreateControllerWithMockedManager(mockContactManager);

            // Act
            var result = await controller.Delete(contactId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("An unexpected error occurred while deactivating the contact.", problemDetails.Detail);
        }

        #endregion Delete contact
    }
}