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
        var result = await ContactRepository.AddAsync(contact);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Id >= 0, "savedRec.Id is greater then 0");
        Assert.Equal(contact.Id, result.Value.Id);
    }

    [Fact]
    public async Task Can_Delete_Contact_From_DataSource()
    {
        // Arrange
        var contact = SampleDataGenerator.GetContactDao();
        var addResult = await ContactRepository.AddAsync(contact);

        // Act
        var result = await ContactRepository.DeleteAsync(addResult.Value);

        // Assert
        Assert.True(result.Success);
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
        Assert.False(result.Success);
        Assert.True(result.Messages.Count > 0);
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
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.IsActivated);

        // Arrange - update description
        contact.Description = "new testing contact";

        // Act
        result = await ContactRepository.UpdateAsync(contact);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal("new testing contact", result.Value.Description);
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