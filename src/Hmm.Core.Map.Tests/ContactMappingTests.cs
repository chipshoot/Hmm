// Ignore Spelling: Dao

using AutoMapper;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.TestHelp;

namespace Hmm.Core.Map.Tests
{
    public class ContactMappingTests
    {
        private readonly IMapper _mapper;

        public ContactMappingTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<HmmMappingProfile>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void Can_Map_ContactDao_To_Contact()
        {
            // Arrange
            var contactDao = SampleDataGenerator.GetContactDao();

            // Act
            var contact = _mapper.Map<Contact>(contactDao);

            // Assert
            Assert.NotNull(contact);
        }

        [Fact]
        public void Can_Map_Invalid_ContactInfo_To_Contact_With_Empty_ContactInfo()
        {
            // Arrange
            var contactDao = SampleDataGenerator.GetContactDao();
            contactDao.Contact = string.Empty;

            // Act
            var contact = _mapper.Map<Contact>(contactDao);

            // Assert
            Assert.NotNull(contact);
            Assert.Empty(contact.FirstName);
            Assert.Empty(contact.LastName);
            Assert.Empty(contact.Emails);
            Assert.Empty(contact.Phones);
            Assert.Empty(contact.Addresses);
        }

        [Fact]
        public void Can_Map_Contact_To_ContactDao()
        {
            // Arrange
            var contact = SampleDataGenerator.GetContact();

            // Act
            var contactDao = _mapper.Map<ContactDao>(contact);

            // Assert
            Assert.NotNull(contactDao);
        }
    }
}