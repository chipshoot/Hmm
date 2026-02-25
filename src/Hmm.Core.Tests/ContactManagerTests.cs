using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.Map;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using Hmm.Utility.Validation;
using Moq;
using Xunit;

namespace Hmm.Core.Tests
{
    public class ContactManagerTests : CoreTestFixtureBase
    {
        private readonly ContactManager _contactManager;
        private readonly ContactManager _contactManagerWithRealValidator;
        private readonly Mock<IHmmValidator<Contact>> _mockValidator;

        public ContactManagerTests()
        {
            _mockValidator = new Mock<IHmmValidator<Contact>>();
            _mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<Contact>()))
                .ReturnsAsync(ProcessingResult<Contact>.Ok(It.IsAny<Contact>()));
            _contactManager = new ContactManager(ContactRepository, UnitOfWork, Mapper, LookupRepository, _mockValidator.Object);
            _contactManagerWithRealValidator = new ContactManager(ContactRepository, UnitOfWork, Mapper, LookupRepository, new ContactValidator());
        }

        [Fact]
        public async Task Can_Get_Contact()
        {
            // Act
            var contactsResult = await _contactManager.GetContactsAsync();

            // Assert
            Assert.True(contactsResult.Success);
            Assert.NotNull(contactsResult.Value);
            Assert.True(contactsResult.Value.Count >= 1);
        }

        [Fact]
        public async Task Can_Add_Valid_Contact()
        {
            // Arrange
            var contact = SampleDataGenerator.GetContact();
            contact.FirstName = "Test First Name";
            contact.LastName = "Test Last Name";

            // Act
            var newContactResult = await _contactManager.CreateAsync(contact);

            // Assert
            Assert.True(newContactResult.Success);
            Assert.NotNull(newContactResult.Value);
            Assert.True(newContactResult.Value.Id >= 0, "newContact.Id is greater to 0");
        }

        [Fact]
        public async Task Cannot_Add_InValid_Contact()
        {
            // Arrange
            var contact = SampleDataGenerator.GetContact();
            contact.FirstName = GetRandomString(255);
            contact.LastName = "Test Last Name";

            // Act
            var newContactResult = await _contactManagerWithRealValidator.CreateAsync(contact);

            // Assert
            Assert.Null(newContactResult.Value);
            Assert.False(newContactResult.Success);
            Assert.Contains("'First Name' must be between 1 and 200 characters", newContactResult.Messages[0].Message);
        }

        [Fact]
        public async Task Can_Update_Valid_Contact()
        {
            // Arrange
            var contactsResult = await _contactManager.GetContactsAsync();
            var contact = contactsResult.Value.FirstOrDefault();
            Assert.NotNull(contact);
            Assert.True(contact.Id > 0, "contact.Id is greater then 0");
            Assert.True(contact.IsActivated);

            // Act
            contact.FirstName = "Updated FirstName";
            var updatedContactResult = await _contactManager.UpdateAsync(contact);

            // Assert
            Assert.NotNull(updatedContactResult.Value);
            Assert.Equal("Updated FirstName", updatedContactResult.Value.FirstName);
            Assert.True(updatedContactResult.Success);
            Assert.Empty(updatedContactResult.Messages);
        }

        [Fact]
        public async Task Cannot_Update_InValid_Contact()
        {
            //    Arrange
            var contact = SampleDataGenerator.GetContact();
            var createResult = await _contactManager.CreateAsync(contact);
            Assert.True(createResult.Success, "Contact should be created successfully");
            Assert.True(createResult.Value.Id > 0, "new contact.Id is greater then 0");

            //   Act
            var contactToUpdate = createResult.Value;
            contactToUpdate.LastName = GetRandomString(255);
            var newContactResult = await _contactManagerWithRealValidator.UpdateAsync(contactToUpdate);

            //  Assert
            Assert.False(newContactResult.Success);
            Assert.Contains("'Last Name' must be between 1 and 200 characters", newContactResult.Messages[0].Message);
            Assert.Null(newContactResult.Value);
        }

        [Fact]
        public async Task Cannot_Update_Not_Exists_Contact()
        {
            // Arrange - no id
            var contact = SampleDataGenerator.GetContact();
            contact.Id = 0;

            //   Act
            var newContactResult = await _contactManager.UpdateAsync(contact);

            //  Assert
            Assert.False(newContactResult.Success);
            Assert.Null(newContactResult.Value);

            // Arrange - id not exist
            contact = new Contact
            {
                Id = 200,
                IsActivated = true,
            };

            // Act
            newContactResult = await _contactManager.UpdateAsync(contact);

            //  Assert
            Assert.False(newContactResult.Success);
            Assert.Null(newContactResult.Value);
        }

        [Fact]
        public async Task Can_Deactivate_Contact()
        {
            // Arrange
            var contactsResult = await _contactManager.GetContactsAsync();
            var contact = contactsResult.Value.FirstOrDefault();
            Assert.NotNull(contact);
            Assert.True(contact.IsActivated);

            // Act
            var deactivateResult = await _contactManager.DeActivateAsync(contact.Id);
            var updatedContactResult = await _contactManager.GetContactByIdAsync(contact.Id);

            // Assert
            Assert.True(deactivateResult.Success);
            Assert.False(updatedContactResult.Success);
            Assert.Equal(ErrorCategory.Deleted, updatedContactResult.ErrorType);
            Assert.Null(updatedContactResult.Value);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(100, true)]
        public async Task Can_Check_Contact_Exists(int contactId, bool expected)
        {
            // Arrange
            // Act
            var result = await _contactManager.IsContactExistsAsync(contactId);

            // Assert
            Assert.Equal(result, expected);
        }
    }
}