using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity;
using Hmm.Utility.Dal.Query;
using System;
using System.Collections.Generic;
using System.Dynamic;
using Xunit;

namespace Hmm.ServiceApi.Core.Tests.Filters;

public class FilterExtensionsTests
{
    #region Test Classes

    private class TestEntity : ApiEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    private class AnotherEntity : ApiEntity
    {
        public Guid UniqueId { get; set; }
        public string Title { get; set; }
        public int Count { get; set; }
    }

    #endregion

    #region ShapeData<T> (Single Object) Tests

    [Fact]
    public void ShapeData_NullSource_ThrowsArgumentNullException()
    {
        // Arrange
        TestEntity source = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => source.ShapeData(null));
    }

    [Fact]
    public void ShapeData_NoFields_ReturnsAllPropertiesExceptLinks()
    {
        // Arrange
        var source = new TestEntity
        {
            Id = 1,
            Name = "Test",
            Description = "Test Description",
            CreatedDate = DateTime.UtcNow
        };

        // Act
        var result = source.ShapeData(null);

        // Assert
        var dict = (IDictionary<string, object>)result;
        Assert.True(dict.ContainsKey("Id"));
        Assert.True(dict.ContainsKey("Name"));
        Assert.True(dict.ContainsKey("Description"));
        Assert.True(dict.ContainsKey("CreatedDate"));
        Assert.False(dict.ContainsKey("Links")); // Links should be excluded
        Assert.Equal(1, dict["Id"]);
        Assert.Equal("Test", dict["Name"]);
    }

    [Fact]
    public void ShapeData_EmptyFields_ReturnsAllPropertiesExceptLinks()
    {
        // Arrange
        var source = new TestEntity { Id = 1, Name = "Test" };

        // Act
        var result = source.ShapeData("");

        // Assert
        var dict = (IDictionary<string, object>)result;
        Assert.True(dict.ContainsKey("Id"));
        Assert.True(dict.ContainsKey("Name"));
        Assert.False(dict.ContainsKey("Links"));
    }

    [Fact]
    public void ShapeData_SpecificFields_ReturnsOnlyRequestedProperties()
    {
        // Arrange
        var source = new TestEntity
        {
            Id = 1,
            Name = "Test",
            Description = "Test Description",
            CreatedDate = DateTime.UtcNow
        };

        // Act
        var result = source.ShapeData("Id,Name");

        // Assert
        var dict = (IDictionary<string, object>)result;
        Assert.Equal(2, dict.Count);
        Assert.True(dict.ContainsKey("Id"));
        Assert.True(dict.ContainsKey("Name"));
        Assert.False(dict.ContainsKey("Description"));
        Assert.False(dict.ContainsKey("CreatedDate"));
    }

    [Fact]
    public void ShapeData_FieldsWithSpaces_TrimsAndReturnsCorrectProperties()
    {
        // Arrange
        var source = new TestEntity { Id = 1, Name = "Test", Description = "Desc" };

        // Act
        var result = source.ShapeData("  Id  ,  Name  ");

        // Assert
        var dict = (IDictionary<string, object>)result;
        Assert.Equal(2, dict.Count);
        Assert.True(dict.ContainsKey("Id"));
        Assert.True(dict.ContainsKey("Name"));
    }

    [Fact]
    public void ShapeData_CaseInsensitiveFields_ReturnsCorrectProperties()
    {
        // Arrange
        var source = new TestEntity { Id = 1, Name = "Test" };

        // Act
        var result = source.ShapeData("id,NAME");

        // Assert
        var dict = (IDictionary<string, object>)result;
        Assert.Equal(2, dict.Count);
        Assert.True(dict.ContainsKey("Id") || dict.ContainsKey("id"));
        Assert.True(dict.ContainsKey("Name") || dict.ContainsKey("NAME"));
    }

    [Fact]
    public void ShapeData_InvalidField_ThrowsException()
    {
        // Arrange
        var source = new TestEntity { Id = 1, Name = "Test" };

        // Act & Assert
        var ex = Assert.Throws<Exception>(() => source.ShapeData("Id,NonExistentField"));
        Assert.Contains("NonExistentField", ex.Message);
        Assert.Contains("wasn't found", ex.Message);
    }

    #endregion

    #region ShapeData<T> (PageList) Tests

    [Fact]
    public void ShapeData_PageList_NullSource_ThrowsArgumentNullException()
    {
        // Arrange
        PageList<TestEntity> source = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => source.ShapeData(null));
    }

    [Fact]
    public void ShapeData_PageList_NoFields_ReturnsAllProperties()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", Description = "Desc1" },
            new() { Id = 2, Name = "Test2", Description = "Desc2" }
        };
        var source = new PageList<TestEntity>(items, 2, 1, 10);

        // Act
        var result = source.ShapeData(null);

        // Assert
        Assert.Equal(2, result.Count);
        var firstItem = (IDictionary<string, object>)result[0];
        Assert.True(firstItem.ContainsKey("Id"));
        Assert.True(firstItem.ContainsKey("Name"));
        Assert.True(firstItem.ContainsKey("Description"));
    }

    [Fact]
    public void ShapeData_PageList_SpecificFields_ReturnsOnlyRequestedPropertiesWithLinks()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1" },
            new() { Id = 2, Name = "Test2" }
        };
        var source = new PageList<TestEntity>(items, 2, 1, 10);

        // Act
        var result = source.ShapeData("Id");

        // Assert
        Assert.Equal(2, result.Count);
        var firstItem = (IDictionary<string, object>)result[0];
        // Should have Id plus Links (if Links property exists)
        Assert.True(firstItem.ContainsKey("Id"));
        Assert.Equal(1, firstItem["Id"]);
    }

    [Fact]
    public void ShapeData_PageList_PreservesPaginationInfo()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1" },
            new() { Id = 2, Name = "Test2" }
        };
        var source = new PageList<TestEntity>(items, 100, 3, 10);

        // Act
        var result = source.ShapeData("Id");

        // Assert
        Assert.Equal(100, result.TotalCount);
        Assert.Equal(3, result.CurrentPage);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public void ShapeData_PageList_InvalidField_ThrowsException()
    {
        // Arrange
        var items = new List<TestEntity> { new() { Id = 1 } };
        var source = new PageList<TestEntity>(items, 1, 1, 10);

        // Act & Assert
        var ex = Assert.Throws<Exception>(() => source.ShapeData("InvalidProperty"));
        Assert.Contains("InvalidProperty", ex.Message);
    }

    #endregion

    #region Caching Tests

    [Fact]
    public void ShapeData_MultipleCalls_UsesCachedPropertyInfo()
    {
        // This test verifies that caching is working by calling ShapeData multiple times
        // and ensuring consistent results (if caching was broken, results might differ)

        // Arrange
        var source1 = new TestEntity { Id = 1, Name = "Test1" };
        var source2 = new TestEntity { Id = 2, Name = "Test2" };
        var source3 = new TestEntity { Id = 3, Name = "Test3" };

        // Act - Call multiple times to ensure cache is used
        var result1 = source1.ShapeData("Id,Name");
        var result2 = source2.ShapeData("Id,Name");
        var result3 = source3.ShapeData("Id,Name");

        // Assert - All results should have same structure
        var dict1 = (IDictionary<string, object>)result1;
        var dict2 = (IDictionary<string, object>)result2;
        var dict3 = (IDictionary<string, object>)result3;

        Assert.Equal(dict1.Keys.Count, dict2.Keys.Count);
        Assert.Equal(dict2.Keys.Count, dict3.Keys.Count);
        Assert.Equal(1, dict1["Id"]);
        Assert.Equal(2, dict2["Id"]);
        Assert.Equal(3, dict3["Id"]);
    }

    [Fact]
    public void ShapeData_DifferentTypes_CachesPropertyInfoSeparately()
    {
        // Arrange
        var testEntity = new TestEntity { Id = 1, Name = "Test" };
        var anotherEntity = new AnotherEntity { UniqueId = Guid.NewGuid(), Title = "Title" };

        // Act
        var result1 = testEntity.ShapeData(null);
        var result2 = anotherEntity.ShapeData(null);

        // Assert - Each type should have its own cached properties
        var dict1 = (IDictionary<string, object>)result1;
        var dict2 = (IDictionary<string, object>)result2;

        Assert.True(dict1.ContainsKey("Id"));
        Assert.True(dict1.ContainsKey("Name"));
        Assert.False(dict1.ContainsKey("UniqueId"));
        Assert.False(dict1.ContainsKey("Title"));

        Assert.True(dict2.ContainsKey("UniqueId"));
        Assert.True(dict2.ContainsKey("Title"));
        Assert.False(dict2.ContainsKey("Id"));
        Assert.False(dict2.ContainsKey("Name"));
    }

    [Fact]
    public void ShapeData_PageList_MultipleCalls_UsesCachedPropertyInfo()
    {
        // Arrange
        var items1 = new List<TestEntity> { new() { Id = 1, Name = "A" } };
        var items2 = new List<TestEntity> { new() { Id = 2, Name = "B" } };
        var source1 = new PageList<TestEntity>(items1, 1, 1, 10);
        var source2 = new PageList<TestEntity>(items2, 1, 1, 10);

        // Act
        var result1 = source1.ShapeData("Id");
        var result2 = source2.ShapeData("Id");

        // Assert
        var dict1 = (IDictionary<string, object>)result1[0];
        var dict2 = (IDictionary<string, object>)result2[0];

        Assert.Equal(1, dict1["Id"]);
        Assert.Equal(2, dict2["Id"]);
    }

    #endregion
}
