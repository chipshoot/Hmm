using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hmm.Core.DefaultManager;
using Hmm.Core.Map;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.TestHelp;
using Hmm.Utility.Validation;
using Moq;
using Xunit;

namespace Hmm.Core.Tests
{
    public class ContactManagerTests : CoreTestFixtureBase
    {
        private readonly ContactManager _contactManager;

        public ContactManagerTests()
        {
            _contactManager = new ContactManager(ContactRepository, Mapper, LookupRepository, Mock.Of<IHmmValidator<Contact>>());
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
            var newContactResult = await _contactManager.CreateAsync(contact);

            // Assert
            Assert.Null(newContactResult.Value);
            Assert.False(newContactResult.Success);
            Assert.Equal("FirstName : 'First Name' must be between 1 and 200 characters. You entered 255 characters.",
                newContactResult.Messages.First().Message);
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
            await _contactManager.CreateAsync(contact);
            Assert.True(contact.Id > 0, "new contact.Id is greater then 0");

            //   Act
            contact.LastName = GetRandomString(255);
            var newContactResult = await _contactManager.UpdateAsync(contact);

            //  Assert
            Assert.False(newContactResult.Success);
            Assert.True(newContactResult.Messages.FirstOrDefault()?.Message.Contains("LastName : 'Last Name' must be between 1 and 200 characters. You entered 255 characters"));
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
            await _contactManager.DeActivateAsync(contact.Id);
            var updatedContactResult = await _contactManager.GetContactByIdAsync(contact.Id);

            // Assert
            Assert.True(updatedContactResult.Success);
            Assert.Empty(updatedContactResult.Messages);
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