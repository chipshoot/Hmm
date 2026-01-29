using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Tests;

public class DeactivationHelperTests
{
    private readonly Mock<IRepository<TestActivatableEntity>> _mockRepository;
    private readonly Mock<IEntityLookup> _mockLookup;

    public DeactivationHelperTests()
    {
        _mockRepository = new Mock<IRepository<TestActivatableEntity>>();
        _mockLookup = new Mock<IEntityLookup>();
    }

    [Fact]
    public async Task DeactivateAsync_EntityNotFound_ReturnsNotFoundResult()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetEntityAsync(1))
            .ReturnsAsync(ProcessingResult<TestActivatableEntity>.NotFound("Not found"));

        // Act
        var result = await DeactivationHelper.DeactivateAsync(_mockRepository.Object, 1, "test");

        // Assert
        Assert.False(result.Success);
        Assert.True(result.IsNotFound);
        Assert.Contains("Cannot find test with id: 1", result.ErrorMessage);
    }

    [Fact]
    public async Task DeactivateAsync_AlreadyDeactivated_ReturnsOkWithMessage()
    {
        // Arrange
        var entity = new TestActivatableEntity { Id = 1, IsActivated = false };
        _mockRepository.Setup(r => r.GetEntityAsync(1))
            .ReturnsAsync(ProcessingResult<TestActivatableEntity>.Ok(entity));

        // Act
        var result = await DeactivationHelper.DeactivateAsync(_mockRepository.Object, 1, "test");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.HasInfo);
        Assert.Contains(result.Messages, m => m.Message.Contains("already deactivated"));
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<TestActivatableEntity>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateAsync_UpdateFails_ReturnsFailResult()
    {
        // Arrange
        var entity = new TestActivatableEntity { Id = 1, IsActivated = true };
        _mockRepository.Setup(r => r.GetEntityAsync(1))
            .ReturnsAsync(ProcessingResult<TestActivatableEntity>.Ok(entity));
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<TestActivatableEntity>()))
            .ReturnsAsync(ProcessingResult<TestActivatableEntity>.Fail("Update failed"));

        // Act
        var result = await DeactivationHelper.DeactivateAsync(_mockRepository.Object, 1, "test");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Update failed", result.ErrorMessage);
    }

    [Fact]
    public async Task DeactivateAsync_Success_SetsIsActivatedFalseAndReturnsOk()
    {
        // Arrange
        var entity = new TestActivatableEntity { Id = 1, IsActivated = true };
        _mockRepository.Setup(r => r.GetEntityAsync(1))
            .ReturnsAsync(ProcessingResult<TestActivatableEntity>.Ok(entity));
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<TestActivatableEntity>()))
            .ReturnsAsync(ProcessingResult<TestActivatableEntity>.Ok(entity));

        // Act
        var result = await DeactivationHelper.DeactivateAsync(_mockRepository.Object, 1, "test");

        // Assert
        Assert.True(result.Success);
        Assert.False(entity.IsActivated);
        Assert.True(result.HasInfo);
        Assert.Contains(result.Messages, m => m.Message.Contains("has been deactivated"));
    }

    [Fact]
    public async Task DeactivateAsync_WithCommitAction_CallsCommitAfterUpdate()
    {
        // Arrange
        var entity = new TestActivatableEntity { Id = 1, IsActivated = true };
        var commitCalled = false;
        _mockRepository.Setup(r => r.GetEntityAsync(1))
            .ReturnsAsync(ProcessingResult<TestActivatableEntity>.Ok(entity));
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<TestActivatableEntity>()))
            .ReturnsAsync(ProcessingResult<TestActivatableEntity>.Ok(entity));

        // Act
        var result = await DeactivationHelper.DeactivateAsync(
            _mockRepository.Object, 1, "test", () => { commitCalled = true; return Task.CompletedTask; });

        // Assert
        Assert.True(result.Success);
        Assert.True(commitCalled);
    }

    [Fact]
    public async Task DeactivateAsync_ExceptionThrown_ReturnsExceptionResult()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetEntityAsync(1))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var result = await DeactivationHelper.DeactivateAsync(_mockRepository.Object, 1, "test");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Test exception", result.ErrorMessage);
    }

    [Fact]
    public async Task DeactivateAsync_NullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            DeactivationHelper.DeactivateAsync<TestActivatableEntity>(null, 1, "test"));
    }

    [Fact]
    public async Task DeactivateAsync_NullEntityName_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            DeactivationHelper.DeactivateAsync(_mockRepository.Object, 1, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeactivateAsync_EmptyOrWhitespaceEntityName_ThrowsArgumentException(string entityName)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            DeactivationHelper.DeactivateAsync(_mockRepository.Object, 1, entityName));
    }

    // Tests for the overload with IEntityLookup

    [Fact]
    public async Task DeactivateAsync_WithLookup_EntityNotFound_ReturnsNotFoundResult()
    {
        // Arrange
        _mockLookup.Setup(l => l.GetEntityAsync<TestActivatableEntity>(1))
            .ReturnsAsync(ProcessingResult<TestActivatableEntity>.NotFound("Not found"));

        // Act
        var result = await DeactivationHelper.DeactivateAsync(
            _mockLookup.Object, _mockRepository.Object, 1, "test");

        // Assert
        Assert.False(result.Success);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task DeactivateAsync_WithLookup_Success_UsesLookupForGetAndRepositoryForUpdate()
    {
        // Arrange
        var entity = new TestActivatableEntity { Id = 1, IsActivated = true };
        _mockLookup.Setup(l => l.GetEntityAsync<TestActivatableEntity>(1))
            .ReturnsAsync(ProcessingResult<TestActivatableEntity>.Ok(entity));
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<TestActivatableEntity>()))
            .ReturnsAsync(ProcessingResult<TestActivatableEntity>.Ok(entity));

        // Act
        var result = await DeactivationHelper.DeactivateAsync(
            _mockLookup.Object, _mockRepository.Object, 1, "test");

        // Assert
        Assert.True(result.Success);
        _mockLookup.Verify(l => l.GetEntityAsync<TestActivatableEntity>(1), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(entity), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_WithLookup_NullLookup_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            DeactivationHelper.DeactivateAsync<TestActivatableEntity>(
                null, _mockRepository.Object, 1, "test"));
    }

    /// <summary>
    /// Test entity that implements IActivatable for testing purposes.
    /// </summary>
    public class TestActivatableEntity : Entity, IActivatable
    {
        public bool IsActivated { get; set; }
    }
}
