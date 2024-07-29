// Ignore Spelling: Dao

using Hmm.Utility.TestHelp;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Dal.EF.Tests;

public class ContactDaoRepositoryTests : DbTestFixtureBase, IAsyncLifetime
{
    [Fact]
    public async Task Can_Add_Contact_To_DataSource()
    {
        // Arrange
        var contact = SampleDataGenerator.GetContactDao();

        // Act
        var savedRec = await ContactRepository.AddAsync(contact);

        // Assert
        Assert.NotNull(savedRec);
        Assert.True(savedRec.Id >= 0, "savedRec.Id is greater then 0");
        Assert.Equal(contact.Id, savedRec.Id);
    }

    [Fact]
    public async Task Can_Delete_Contact_From_DataSource()
    {
        // Arrange
        var contact = SampleDataGenerator.GetContactDao();
        var savedContact = await ContactRepository.AddAsync(contact);

        // Act
        var result = await ContactRepository.DeleteAsync(savedContact);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task Cannot_Delete_NonExists_Contact_From_DataSource()
    {
        // Arrange
        var contact1 = SampleDataGenerator.GetContactDao();
        await ContactRepository.AddAsync(contact1);

        var contact2 = SampleDataGenerator.GetContactDao();
        contact2.Id = 200;
        contact2.Description = "testing contact 2";

        // Act
        var result = await ContactRepository.DeleteAsync(contact2);

        // Assert
        Assert.False(result);
        Assert.False(ContactRepository.ProcessMessage.Success);
        Assert.Single(ContactRepository.ProcessMessage.MessageList);
    }

    [Fact]
    public async Task Can_Update_Contact()
    {
        // Arrange - update first name
        var contact = SampleDataGenerator.GetContactDao();
        await ContactRepository.AddAsync(contact);

        // Arrange - activate status
        contact.IsActivated = false;

        // Act
        var result = await ContactRepository.UpdateAsync(contact);

        // Arrange
        Assert.NotNull(result);
        Assert.False(result.IsActivated);

        // Arrange - update description
        contact.Description = "new testing contact";

        // Act
        result = await ContactRepository.UpdateAsync(contact);

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