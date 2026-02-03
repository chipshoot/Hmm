using Hmm.Utility.Dal.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Hmm.Utility.Tests
{
    /// <summary>
    /// Tests for PageList verifying the composition-based implementation.
    ///
    /// Issue #61 Resolution: PageList now uses composition instead of inheritance from List&lt;T&gt;.
    /// - Implements IReadOnlyList&lt;T&gt; for read-only access
    /// - No exposed Add, Remove, Clear, or other mutation methods
    /// - Encapsulates internal storage completely
    /// - Pagination metadata is preserved correctly
    ///
    /// Benefits:
    /// - Read-only: Cannot accidentally modify paginated results
    /// - Safe: No mutation surprises
    /// - Clean API: Only exposes what's needed for pagination scenarios
    /// </summary>
    public class PageListTests
    {
        #region Constructor Tests

        [Fact]
        public void DefaultConstructor_CreatesEmptyPageList()
        {
            // Act
            var pageList = new PageList<string>();

            // Assert
            Assert.Empty(pageList);
            Assert.Equal(0, pageList.Count);
            Assert.Equal(0, pageList.TotalCount);
            Assert.Equal(1, pageList.PageSize);
            Assert.Equal(0, pageList.CurrentPage);
            Assert.Equal(1, pageList.TotalPages);
        }

        [Fact]
        public void Constructor_WithItems_PreservesPaginationMetadata()
        {
            // Arrange
            var items = new[] { "a", "b", "c" };
            int totalCount = 100;
            int pageIndex = 2;
            int pageSize = 10;

            // Act
            var pageList = new PageList<string>(items, totalCount, pageIndex, pageSize);

            // Assert
            Assert.Equal(3, pageList.Count);
            Assert.Equal(100, pageList.TotalCount);
            Assert.Equal(2, pageList.CurrentPage);
            Assert.Equal(10, pageList.PageSize);
            Assert.Equal(10, pageList.TotalPages);
        }

        [Fact]
        public void Constructor_WithNullItems_CreatesEmptyList()
        {
            // Act
            var pageList = new PageList<string>(null, 0, 1, 10);

            // Assert
            Assert.Empty(pageList);
            Assert.Equal(0, pageList.Count);
        }

        #endregion

        #region IReadOnlyList Implementation Tests

        [Fact]
        public void Indexer_ReturnsCorrectItem()
        {
            // Arrange
            var items = new[] { "first", "second", "third" };
            var pageList = new PageList<string>(items, 3, 1, 10);

            // Act & Assert
            Assert.Equal("first", pageList[0]);
            Assert.Equal("second", pageList[1]);
            Assert.Equal("third", pageList[2]);
        }

        [Fact]
        public void Count_ReturnsItemsInCurrentPage()
        {
            // Arrange
            var items = new[] { "a", "b", "c" };
            var pageList = new PageList<string>(items, 100, 1, 10);

            // Assert - Count is items in page, not TotalCount
            Assert.Equal(3, pageList.Count);
            Assert.Equal(100, pageList.TotalCount);
        }

        [Fact]
        public void Enumeration_WorksCorrectly()
        {
            // Arrange
            var items = new[] { 1, 2, 3, 4, 5 };
            var pageList = new PageList<int>(items, 5, 1, 5);

            // Act
            var enumerated = new List<int>();
            foreach (var item in pageList)
            {
                enumerated.Add(item);
            }

            // Assert
            Assert.Equal(items, enumerated);
        }

        [Fact]
        public void LinqOperations_WorkCorrectly()
        {
            // Arrange
            var items = new[] { 1, 2, 3, 4, 5 };
            var pageList = new PageList<int>(items, 5, 1, 5);

            // Act & Assert
            Assert.True(pageList.Any());
            Assert.Equal(5, pageList.Count());
            Assert.Equal(15, pageList.Sum());
            Assert.Equal(3, pageList.Average());
            Assert.Contains(3, pageList);
            Assert.DoesNotContain(10, pageList);
        }

        #endregion

        #region Pagination Properties Tests

        [Fact]
        public void HasPrevPage_FirstPage_ReturnsFalse()
        {
            // Arrange
            var pageList = new PageList<int>(new[] { 1 }, 100, 1, 10);

            // Assert
            Assert.False(pageList.HasPrevPage);
        }

        [Fact]
        public void HasPrevPage_SecondPage_ReturnsTrue()
        {
            // Arrange
            var pageList = new PageList<int>(new[] { 1 }, 100, 2, 10);

            // Assert
            Assert.True(pageList.HasPrevPage);
        }

        [Fact]
        public void HasNextPage_LastPage_ReturnsFalse()
        {
            // Arrange
            var pageList = new PageList<int>(new[] { 1 }, 100, 10, 10);

            // Assert
            Assert.False(pageList.HasNextPage);
        }

        [Fact]
        public void HasNextPage_FirstPage_ReturnsTrue()
        {
            // Arrange
            var pageList = new PageList<int>(new[] { 1 }, 100, 1, 10);

            // Assert
            Assert.True(pageList.HasNextPage);
        }

        [Fact]
        public void TotalPages_CalculatedCorrectly()
        {
            // Assert - 100 items / 10 per page = 10 pages
            Assert.Equal(10, new PageList<int>(Array.Empty<int>(), 100, 1, 10).TotalPages);

            // Assert - 101 items / 10 per page = 11 pages (ceiling)
            Assert.Equal(11, new PageList<int>(Array.Empty<int>(), 101, 1, 10).TotalPages);

            // Assert - 99 items / 10 per page = 10 pages (ceiling)
            Assert.Equal(10, new PageList<int>(Array.Empty<int>(), 99, 1, 10).TotalPages);

            // Assert - 1 item / 10 per page = 1 page
            Assert.Equal(1, new PageList<int>(Array.Empty<int>(), 1, 1, 10).TotalPages);
        }

        #endregion

        #region Create Static Method Tests

        [Fact]
        public void Create_FromQueryable_ReturnsCorrectPage()
        {
            // Arrange
            var source = Enumerable.Range(1, 100).AsQueryable();

            // Act
            var pageList = PageList<int>.Create(source, 1, 10);

            // Assert
            Assert.Equal(10, pageList.Count);
            Assert.Equal(100, pageList.TotalCount);
            Assert.Equal(1, pageList.CurrentPage);
            Assert.Equal(10, pageList.PageSize);
            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, pageList.ToArray());
        }

        [Fact]
        public void Create_SecondPage_ReturnsCorrectItems()
        {
            // Arrange
            var source = Enumerable.Range(1, 100).AsQueryable();

            // Act
            var pageList = PageList<int>.Create(source, 2, 10);

            // Assert
            Assert.Equal(10, pageList.Count);
            Assert.Equal(new[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }, pageList.ToArray());
        }

        [Fact]
        public void Create_InvalidPageIndex_ThrowsArgumentException()
        {
            // Arrange
            var source = Enumerable.Range(1, 100).AsQueryable();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => PageList<int>.Create(source, 0, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => PageList<int>.Create(source, -1, 10));
        }

        [Fact]
        public void Create_InvalidPageSize_ThrowsArgumentException()
        {
            // Arrange
            var source = Enumerable.Range(1, 100).AsQueryable();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => PageList<int>.Create(source, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => PageList<int>.Create(source, 1, -1));
        }

        #endregion

        #region Immutability Tests

        [Fact]
        public void PageList_DoesNotExposeListMethods()
        {
            // This test verifies at compile time that mutation methods are not available
            // If any of these uncommented, it would fail to compile
            var pageList = new PageList<int>(new[] { 1, 2, 3 }, 3, 1, 10);

            // The following operations are NOT available (by design):
            // pageList.Add(4);           // Compile error
            // pageList.Remove(1);        // Compile error
            // pageList.Clear();          // Compile error
            // pageList.Insert(0, 0);     // Compile error
            // pageList.RemoveAt(0);      // Compile error
            // pageList[0] = 99;          // Compile error (indexer is read-only)

            // Only read operations are available
            Assert.Equal(3, pageList.Count);
            Assert.Equal(1, pageList[0]);
            Assert.True(pageList.Any());
        }

        [Fact]
        public void PageList_ImplementsIReadOnlyList()
        {
            // Arrange
            var pageList = new PageList<string>(new[] { "a", "b" }, 2, 1, 10);

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<string>>(pageList);
            Assert.IsAssignableFrom<IEnumerable<string>>(pageList);
        }

        #endregion
    }
}
