// Ignore Spelling: Dao

using AutoMapper;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;

namespace Hmm.Core.Map.Tests;

public class AuthorMappingTests
{
    private readonly IMapper _mapper;

    public AuthorMappingTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HmmMappingProfile>();
        });
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Can_Map_AuthorDao_To_Author()
    {
        // Arrange
        var authorDao = new AuthorDao
        {
            Id = 100,
            AccountName = "fchy",
            ContactInfo = new ContactDao
            {
                Id = 200,
                Contact = """
                          { "FirstName": "John", "LastName": "Doe", "Emails": [ { "Address": "fchy@yahoo.com", "Type": 0, "IsPrimary": true }, { "Address": "fchy5979@gamil.com", "Type": 0, "IsPrimary": false }, { "Address": "fchy@outlook.com", "Type": 1, "IsPrimary": false } ], "Phones": [ { "Number": "123-456-7890", "Type": 0, "IsPrimary": true }, { "Number": "098-765-4321", "Type": 2, "IsPrimary": false } ], "Addresses": [ { "Address": "123 Main St", "City": "Springfield", "State": "IL", "PostalCode": "12345", "Country": "USA", "Type": 0, "IsPrimary": true }, { "Address": "456 Elm St", "City": "Springfield", "State": "IL", "PostalCode": "12345", "Country": "USA", "Type": 1, "IsPrimary": false } ] }
                          """,
                Description = "testing contact 2",
                IsActivated = true
            },
            Role = DbEntity.AuthorRoleType.Author,
            Description = "Testing Author",
            IsActivated = true
        };

        // Act
        var author = _mapper.Map<Author>(authorDao);

        // Assert
        Assert.NotNull(author);
        Assert.Equal("fchy", author.AccountName);
        Assert.Equal("John", author.ContactInfo.FirstName);
        Assert.Equal("Doe", author.ContactInfo.LastName);
        Assert.Equal("fchy@yahoo.com", author.ContactInfo.Emails.FirstOrDefault()?.Address);
        Assert.Equal(EmailType.Personal, author.ContactInfo.Emails.FirstOrDefault()?.Type);
        Assert.Equal("123-456-7890", author.ContactInfo.Phones.FirstOrDefault()?.Number);
        Assert.Equal(TelephoneType.Home, author.ContactInfo.Phones.FirstOrDefault()?.Type);
        Assert.Equal("123 Main St", author.ContactInfo.Addresses.FirstOrDefault()?.Address);
        Assert.Equal(AddressType.Home, author.ContactInfo.Addresses.FirstOrDefault()?.Type);
        Assert.Equal("Testing Author", author.Description);
        Assert.True(author.IsActivated);
    }

    //[Fact]
    //public void Can_Map_Invalid_ContactInfo_To_Empty_Contact()
    //{
    //    // Arrange
    //    var contactDao = new ContactDao
    //    {
    //        Id = 100,
    //        Contact = """
    //                  { "FirstName": "John", "LastName": "Doe", "Emails": [ { "Address": "fchy@yahoo.com", "Type": 0, "IsPrimary": true }, { "Address": "fchy5979@gamil.com", "Type": 0, "IsPrimary": false }, { "Address": "fchy@outlook.com", "Type": 1, "IsPrimary": false } ], "Phones": [ { "Number": "123-456-7890", "Type": 0, "IsPrimary": true }, { "Number": "098-765-4321", "Type": 2, "IsPrimary": false } ], "Addresses": [ { "Address": "123 Main St", "City": "Springfield", "State": "IL", "PostalCode": "12345", "Country": "USA", "Type": 0, "IsPrimary": true }, { "Address": "456 Elm St", "City": "Springfield", "State": "IL", "PostalCode": "12345", "Country": "USA", "Type": 1, "IsPrimary": false } ] }
    //                  """,
    //        Description = "testing contact 2",
    //        IsActivated = true
    //    };

    //    // Act
    //    var contact = _mapper.Map<Contact>(contactDao);

    //    // Assert
    //    Assert.NotNull(contact);
    //}

    [Fact]
    public void Can_Map_Author_To_AuthorDao()
    {
        // Arrange
        var contact = new Contact
        {
            FirstName = "John",
            LastName = "Doe",
            Emails = new List<Email>
                {
                    new() { Address = "fchy@yahoo.com", Type = EmailType.Personal, IsPrimary = true },
                    new() { Address = "fchy5979@gamil.com", Type = EmailType.Personal, IsPrimary = false },
                    new() { Address = "fchy@outlook.com", Type = EmailType.Work, IsPrimary = false }
                },
            Phones = new List<Phone>
                {
                    new() { Number = "123-456-7890", Type = TelephoneType.Home, IsPrimary = true },
                    new() { Number = "098-765-4321", Type = TelephoneType.Work,  IsPrimary = false }
                },
            Addresses = new List<AddressInfo>
                {
                    new() { Address = "123 Main St", City = "Springfield", State = "IL", PostalCode = "12345", Country = "USA", Type = AddressType.Home, IsPrimary = true},
                    new() { Address = "456 Elm St", City = "Springfield", State = "IL", PostalCode = "12345", Country = "USA", Type = AddressType.Work, IsPrimary = false}
                },
            IsActivated = true,
            Description = "This is a testing contact to be mapped to DAO object"
        };
        var author = new Author
        {
            AccountName = "fchy",
            ContactInfo = contact,
            Role = DomainEntity.AuthorRoleType.Author,
            IsActivated = true
        };

        // Act
        var authorDao = _mapper.Map<AuthorDao>(author);

        // Assert
        Assert.NotNull(authorDao);
        Assert.NotNull(authorDao.ContactInfo);
    }
}