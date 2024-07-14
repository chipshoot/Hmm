// Ignore Spelling: Dao

using Hmm.Core.Dal.EF.DbEntity;
using Hmm.Utility.TestHelp;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Dal.EF.Tests;

public class ContactDaoRepositoryTests : DbTestFixtureBase, IAsyncLifetime
{
    [Fact]
    public void Can_Add_Contact_To_DataSource()
    {
        // Arrange
        var contactDb = new ContactDao
        {
            Contact = """
                      { "FirstName": "John", "LastName": "Doe", "Emails": [ { "Address": "fchy@yahoo.com", "Type": "Personal", "IsPrimary": "false" }, { "Address": "fchy5979@gamil.com", "Type": "Personal", "IsPrimary": "true" }, { "Address": "fchy@outlook.com", "Type": "Work", "IsPrimary": "false" } ], "Phones": [ { "Type": "Home", "Number": "123-456-7890" }, { "Type": "Work", "Number": "456-789-0123" } ], "Addresses": [ { "Type": "Home", "Street": "123 Main St", "City": "Springfield", "State": "IL", "Zip": "62701" }, { "Type": "Work", "Street": "456 Elm St", "City": "Springfield", "State": "IL", "Zip": "62702" } ] }
                      """,
            Description = "testing contact",
            IsActivated = true
        };

        // Act
        var savedRec = ContactRepository.Add(contactDb);

        // Assert
        Assert.NotNull(savedRec);
        Assert.True(savedRec.Id >= 0, "savedRec.Id is greater then 0");
        Assert.Equal(contactDb.Id, savedRec.Id);
    }

    [Fact]
    public void Can_Delete_Contact_From_DataSource()
    {
        // Arrange
        var contact = new ContactDao
        {
            Contact = """
                      { "FirstName": "John", "LastName": "Doe", "Emails": [ { "Address": "fchy@yahoo.com", "Type": "Personal", "IsPrimary": "false" }, { "Address": "fchy5979@gamil.com", "Type": "Personal", "IsPrimary": "true" }, { "Address": "fchy@outlook.com", "Type": "Work", "IsPrimary": "false" } ], "Phones": [ { "Type": "Home", "Number": "123-456-7890" }, { "Type": "Work", "Number": "456-789-0123" } ], "Addresses": [ { "Type": "Home", "Street": "123 Main St", "City": "Springfield", "State": "IL", "Zip": "62701" }, { "Type": "Work", "Street": "456 Elm St", "City": "Springfield", "State": "IL", "Zip": "62702" } ] }
                      """,
            Description = "testing contact",
            IsActivated = true
        };
        var savedContact = ContactRepository.Add(contact);

        // Act
        var result = ContactRepository.Delete(savedContact);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Cannot_Delete_NonExists_Contact_From_DataSource()
    {
        // Arrange
        var contact1 = new ContactDao
        {
            Contact = """
                      { "FirstName": "John", "LastName": "Doe", "Emails": [ { "Address": "fchy@yahoo.com", "Type": "Personal", "IsPrimary": "false" }, { "Address": "fchy5979@gamil.com", "Type": "Personal", "IsPrimary": "true" }, { "Address": "fchy@outlook.com", "Type": "Work", "IsPrimary": "false" } ], "Phones": [ { "Type": "Home", "Number": "123-456-7890" }, { "Type": "Work", "Number": "456-789-0123" } ], "Addresses": [ { "Type": "Home", "Street": "123 Main St", "City": "Springfield", "State": "IL", "Zip": "62701" }, { "Type": "Work", "Street": "456 Elm St", "City": "Springfield", "State": "IL", "Zip": "62702" } ] }
                      """,
            Description = "testing contact 1",
            IsActivated = true
        };
        ContactRepository.Add(contact1);

        var contact2 = new ContactDao
        {
            Id = 1,
            Contact = """
                      { "FirstName": "John", "LastName": "Doe", "Emails": [ { "Address": "fchy@yahoo.com", "Type": "Personal", "IsPrimary": "false" }, { "Address": "fchy5979@gamil.com", "Type": "Personal", "IsPrimary": "true" }, { "Address": "fchy@outlook.com", "Type": "Work", "IsPrimary": "false" } ], "Phones": [ { "Type": "Home", "Number": "123-456-7890" }, { "Type": "Work", "Number": "456-789-0123" } ], "Addresses": [ { "Type": "Home", "Street": "123 Main St", "City": "Springfield", "State": "IL", "Zip": "62701" }, { "Type": "Work", "Street": "456 Elm St", "City": "Springfield", "State": "IL", "Zip": "62702" } ] }
                      """,
            Description = "testing contact 2",
            IsActivated = true
        };

        // Act
        var result = ContactRepository.Delete(contact2);

        // Assert
        Assert.False(result);
        Assert.False(ContactRepository.ProcessMessage.Success);
        Assert.Single(ContactRepository.ProcessMessage.MessageList);
    }

    [Fact]
    public void Can_Update_Contact()
    {
        // Arrange - update first name
        var contact = new ContactDao
        {
            Contact = """
                      { "FirstName": "John", "LastName": "Doe", "Emails": [ { "Address": "fchy@yahoo.com", "Type": "Personal", "IsPrimary": "false" }, { "Address": "fchy5979@gamil.com", "Type": "Personal", "IsPrimary": "true" }, { "Address": "fchy@outlook.com", "Type": "Work", "IsPrimary": "false" } ], "Phones": [ { "Type": "Home", "Number": "123-456-7890" }, { "Type": "Work", "Number": "456-789-0123" } ], "Addresses": [ { "Type": "Home", "Street": "123 Main St", "City": "Springfield", "State": "IL", "Zip": "62701" }, { "Type": "Work", "Street": "456 Elm St", "City": "Springfield", "State": "IL", "Zip": "62702" } ] }
                      """,
            Description = "testing contact",
            IsActivated = true
        };
        ContactRepository.Add(contact);

        // Arrange - activate status
        contact.IsActivated = false;

        // Act
        var result = ContactRepository.Update(contact);

        // Arrange
        Assert.NotNull(result);
        Assert.False(result.IsActivated);

        // Arrange - update description
        contact.Description = "new testing contact";

        // Act
        result = ContactRepository.Update(contact);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new testing contact", result.Description);
    }

    //[Fact]
    //public void Cannot_Update_For_Non_Exists_Author()
    //{
    //    // Arrange
    //    var author = new Author
    //    {
    //        AccountName = "glog",
    //        Description = "testing user",
    //        IsActivated = true
    //    };

    //    AuthorRepository.Add(author);

    //    var author2 = new Author
    //    {
    //        AccountName = "glog2",
    //        Description = "testing user",
    //        IsActivated = true
    //    };

    //    // Act
    //    var result = AuthorRepository.Update(author2);

    //    // Assert
    //    Assert.Null(result);
    //    Assert.False(AuthorRepository.ProcessMessage.Success);
    //    Assert.Single(AuthorRepository.ProcessMessage.MessageList);
    //}

    public async Task InitializeAsync()
    {
        Transaction = await ((DbContext)DbContext).Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        await Transaction.RollbackAsync();
        await Transaction.DisposeAsync();
    }
}