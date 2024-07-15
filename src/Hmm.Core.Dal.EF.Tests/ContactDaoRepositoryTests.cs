// Ignore Spelling: Dao

using Hmm.Core.Map.DbEntity;
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
        var contact = GetTestingContact();

        // Act
        var savedRec = ContactRepository.Add(contact);

        // Assert
        Assert.NotNull(savedRec);
        Assert.True(savedRec.Id >= 0, "savedRec.Id is greater then 0");
        Assert.Equal(contact.Id, savedRec.Id);
    }

    [Fact]
    public void Can_Delete_Contact_From_DataSource()
    {
        // Arrange
        var contact = GetTestingContact();
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
        var contact1 = GetTestingContact();
        ContactRepository.Add(contact1);

        var contact2 = new ContactDao
        {
            Id = 100,
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
        var contact = GetTestingContact();
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