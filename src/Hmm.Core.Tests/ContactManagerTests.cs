using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hmm.Core.DefaultManager;
using Hmm.Core.Map;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.TestHelp;
using Xunit;

namespace Hmm.Core.Tests
{
    public class ContactManagerTests : CoreTestFixtureBase
    {
        private readonly ContactManager _contactManager;

        public ContactManagerTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<HmmMappingProfile>();
            });
            var mapper = config.CreateMapper();
            _contactManager = new ContactManager(ContactRepository, mapper);
        }

        [Fact]
        public async Task Can_Get_Contact()
        {
            // Act
            var contacts = await _contactManager.GetContactsAsync();

            // Assert
            Assert.True(_contactManager.ProcessResult.Success);
            Assert.NotNull(contacts);
            Assert.Single(contacts);
        }

        [Fact]
        public async Task Can_Add_Valid_Contact()
        {
            // Arrange
            var contact = SampleDataGenerator.GetContact();
            contact.FirstName = "Test First Name";
            contact.LastName = "Test Last Name";

            // Act
            var newContact = await _contactManager.CreateAsync(contact);

            // Assert
            Assert.True(_contactManager.ProcessResult.Success);
            Assert.NotNull(newContact);
            Assert.True(newContact.Id >= 0, "newContact.Id is greater to 0");
        }

        [Fact]
        public async Task Cannot_Add_InValid_Contact()
        {
            // Arrange
            var contact = SampleDataGenerator.GetContact();
            contact.FirstName = GetRandomString(255);
            contact.LastName = "Test Last Name";

            // Act
            var newContact = await _contactManager.CreateAsync(contact);

            // Assert
            Assert.Null(newContact);
            Assert.False(_contactManager.ProcessResult.Success);
            Assert.Equal("FirstName : 'First Name' must be between 1 and 200 characters. You entered 255 characters.",
                _contactManager.ProcessResult.MessageList.First().Message);
        }

        [Fact]
        public async Task Can_Update_Valid_Contact()
        {
            // Arrange
            var contacts = await _contactManager.GetContactsAsync();
            var contact = contacts.FirstOrDefault();
            Assert.NotNull(contact);
            Assert.True(contact.Id > 0, "contact.Id is greater then 0");
            Assert.True(contact.IsActivated);

            //   Act
            contact.IsActivated = false;
            var updatedContact = await _contactManager.UpdateAsync(contact);

            //  Assert
            Assert.NotNull(updatedContact);
            Assert.False(updatedContact.IsActivated);
            Assert.True(_contactManager.ProcessResult.Success);
            Assert.Empty(_contactManager.ProcessResult.MessageList);
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
            var newContact = await _contactManager.UpdateAsync(contact);

            //  Assert
            Assert.False(_contactManager.ProcessResult.Success);
            Assert.True(_contactManager.ProcessResult.MessageList.FirstOrDefault()?.Message.Contains("LastName : 'Last Name' must be between 1 and 200 characters. You entered 255 characters"));
            Assert.Null(newContact);
        }

        [Fact]
        public async Task Cannot_Update_Not_Exists_Contact()
        {
            // Arrange - no id
            var contact = SampleDataGenerator.GetContact();
            contact.Id = 0;

            //   Act
            var newContact = await _contactManager.UpdateAsync(contact);

            //  Assert
            Assert.False(_contactManager.ProcessResult.Success);
            Assert.Null(newContact);

            // Arrange - id not exist
            contact = new Contact
            {
                Id = 200,
                IsActivated = true,
            };

            // Act
            newContact = await _contactManager.UpdateAsync(contact);

            //  Assert
            Assert.False(_contactManager.ProcessResult.Success);
            Assert.Null(newContact);
        }

        [Fact]
        public async Task Can_Deactivate_Contact()
        {
            // Arrange
            var contacts = await _contactManager.GetContactsAsync();
            var contact = contacts.FirstOrDefault();
            Assert.NotNull(contact);
            Assert.True(contact.IsActivated);

            // Act
            await _contactManager.DeActivateAsync(contact.Id);
            var updatedContact = await _contactManager.GetContactByIdAsync(contact.Id);

            // Assert
            Assert.True(_contactManager.ProcessResult.Success);
            Assert.Empty(_contactManager.ProcessResult.MessageList);
            Assert.False(updatedContact.IsActivated);
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