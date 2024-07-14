//using Hmm.Core.DefaultManager;
//using Hmm.Core.DomainEntity;
//using Hmm.Utility.TestHelp;
//using System;
//using System.Linq;
//using Xunit;

//namespace Hmm.Core.Tests

//{
//    public class ContactManagerTests : TestFixtureBase
//    {
//        #region private fields

//        private IContactManager _contactManager;

//        #endregion private fields

//        public ContactManagerTests()
//        {
//            SetupTestEnv();
//        }

//        [Fact]
//        public void Can_Get_Contact()
//        {
//            // Act
//            var contacts = _contactManager.GetContacts().ToList();

//            // Assert
//            Assert.True(_contactManager.ProcessResult.Success);
//            Assert.True(contacts.Count > 1, "authors.Count > 1");
//        }

//        [Fact]
//        public void Can_Add_Valid_Contact()
//        {
//            // Arrange
//            var contact = new Contact
//            {
//                AccountName = "jfang2",
//                IsActivated = true
//            };

//            // Act
//            var newContact = _contactManager.Create(contact);

//            // Assert
//            Assert.True(_contactManager.ProcessResult.Success);
//            Assert.NotNull(newContact);
//            Assert.True(newContact.Id >= 0, "newContact.Id is greater to 0");
//        }

//        [Fact]
//        public void Cannot_Add_Invalid_Contact()
//        {
//            // Arrange
//            var contact = new Contact
//            {
//                AccountName = "Test Account Name",
//                IsActivated = true,
//            };

//            // Act
//            var newContact = _contactManager.Create(contact);

//            // Assert
//            Assert.False(_contactManager.ProcessResult.Success);
//            Assert.True(_contactManager.ProcessResult.MessageList.FirstOrDefault()?.Message.Contains("Contact is invalid"));
//            Assert.Null(newContact);
//        }

//        [Fact]
//        public void Can_Update_Valid_Contact()
//        {
//            // Arrange
//            var contact = new Contact
//            {
//                AccountName = "jfang2",
//                Role = AuthorRoleType.Author,
//                IsActivated = true,
//            };
//            var result = _contactManager.Create(contact);
//            Assert.True(contact.Id > 0, "contact.Id is greater then 0");

//            //   Act
//            var savedContact = _contactManager.GetEntities().FirstOrDefault(c => c.Id == result.Id);
//            Assert.NotNull(savedContact);
//            savedContact.Role = AuthorRoleType.Guest;
//            var updatedContact = _contactManager.Update(savedContact);

//            //  Assert
//            Assert.NotNull(updatedContact);
//            Assert.Equal(AuthorRoleType.Guest, updatedContact.Role);
//            Assert.True(_contactManager.ProcessResult.Success);
//            Assert.False(_contactManager.ProcessResult.MessageList.Any());
//        }

//        [Fact]
//        public void Cannot_Update_InValid_Contact()
//        {
//            //    Arrange
//            var contact = new Contact
//            {
//                AccountName = "jfang2",
//                IsActivated = true,
//            };
//            _contactManager.Create(contact);
//            Assert.True(contact.Id > 0, "new contact.Id is greater then 0");

//            //   Act
//            contact.AccountName = "jfang3";
//            var newContact = _contactManager.Update(contact);

//            //  Assert
//            Assert.False(_contactManager.ProcessResult.Success);
//            Assert.True(_contactManager.ProcessResult.MessageList.FirstOrDefault()?.Message.Contains("contact is invalid"));
//            Assert.Null(newContact);
//        }

//        [Fact]
//        public void Cannot_Update_Not_Exists_Contact()
//        {
//            // Arrange - no id
//            var contact = new Contact
//            {
//                AccountName = "jfang2",
//                IsActivated = true,
//            };

//            //   Act
//            var newContact = _contactManager.Update(contact);

//            //  Assert
//            Assert.False(_contactManager.ProcessResult.Success);
//            Assert.Null(newContact);

//            // Arrange - id not exist
//            _contactManager.ProcessResult.Rest();
//            contact = new Contact
//            {
//                Id = 2,
//                AccountName = "jfang2",
//                IsActivated = true,
//            };

//            // Act
//            newContact = _contactManager.Update(contact);

//            //  Assert
//            Assert.False(_contactManager.ProcessResult.Success);
//            Assert.Null(newContact);
//        }

//        [Fact]
//        public void Can_Deactivate_Contact()
//        {
//            // Arrange
//            var contact = _contactManager.GetEntities().FirstOrDefault();
//            Assert.NotNull(contact);
//            Assert.True(contact.IsActivated);

//            // Act
//            _contactManager.DeActivate(contact.Id);

//            // Assert
//            Assert.True(_contactManager.ProcessResult.Success);
//            Assert.False(_contactManager.ProcessResult.MessageList.Any());
//            Assert.False(contact.IsActivated);
//        }

//        [Theory]
//        [InlineData(0, false)]
//        [InlineData(1, true)]
//        public void Can_Check_Contact_Exists(int contactId, bool expected)
//        {
//            // Arrange
//            var id = contactId;
//            if (contactId == 1)
//            {
//                id = LookupRepository.GetEntities<Contact>().FirstOrDefault()?.Id;
//            }

//            // Act
//            var result = _contactManager.ContactExists(id);

//            // Assert
//            Assert.Equal(result, expected);
//        }

//        [Fact]
//        public void Cannot_Check_Contact_Exists_With_Invalid_Id()
//        {
//            // Arrange
//            var id = 0;

//            // Act & Assert
//            Assert.Throws<ArgumentNullException>(() => _contactManager.ContactExists(id));

//            // Arrange
//            id = -1;

//            // Act & Assert
//            Assert.Throws<ArgumentNullException>(() => _contactManager.ContactExists(id));
//        }

//        private void SetupTestEnv()
//        {
//            InsertSeedRecords();
//            _contactManager = new ContactManager(ContactRepository);
//        }
//    }
//}