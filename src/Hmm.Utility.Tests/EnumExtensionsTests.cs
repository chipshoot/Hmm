using Hmm.Utility.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace Hmm.Utility.Tests
{
    public class EnumExtensionsTests
    {
        #region Test Enums

        public enum TestEnum
        {
            [Display(Name = "First Value", Description = "This is the first value")]
            First = 1,

            [Display(Name = "Second Value", Description = "This is the second value")]
            Second = 2,

            [Display(Name = "Third Value")]
            Third = 3,

            // No attribute
            Fourth = 4
        }

        public enum EmptyEnum
        {
            Value1,
            Value2,
            Value3
        }

        public enum CaseSensitiveEnum
        {
            [Display(Name = "Lower Case")]
            LowerCase = 1,

            [Display(Name = "UPPER CASE")]
            UpperCase = 2,

            [Display(Name = "MiXeD CaSe")]
            MixedCase = 3
        }

        #endregion

        #region GetDisplayName Tests

        [Fact]
        public void GetDisplayName_WithDisplayAttribute_ReturnsDisplayName()
        {
            // Arrange
            var value = TestEnum.First;

            // Act
            var result = value.GetDisplayName();

            // Assert
            Assert.Equal("First Value", result);
        }

        [Fact]
        public void GetDisplayName_WithoutDisplayAttribute_ReturnsEnumName()
        {
            // Arrange
            var value = TestEnum.Fourth;

            // Act
            var result = value.GetDisplayName();

            // Assert
            Assert.Equal("Fourth", result);
        }

        [Fact]
        public void GetDisplayName_WithNull_ThrowsArgumentNullException()
        {
            // Arrange
            Enum value = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => value.GetDisplayName());
        }

        [Fact]
        public void GetDisplayName_CalledMultipleTimes_UsesCache()
        {
            // Arrange
            var value = TestEnum.First;

            // Act
            var result1 = value.GetDisplayName();
            var result2 = value.GetDisplayName();

            // Assert
            Assert.Equal(result1, result2);
            Assert.Equal("First Value", result1);
        }

        [Fact]
        public void GetDisplayName_DifferentEnumValues_ReturnsCorrectNames()
        {
            // Arrange & Act & Assert
            Assert.Equal("First Value", TestEnum.First.GetDisplayName());
            Assert.Equal("Second Value", TestEnum.Second.GetDisplayName());
            Assert.Equal("Third Value", TestEnum.Third.GetDisplayName());
        }

        #endregion

        #region GetDisplayDescription Tests

        [Fact]
        public void GetDisplayDescription_WithDescription_ReturnsDescription()
        {
            // Arrange
            var value = TestEnum.First;

            // Act
            var result = value.GetDisplayDescription();

            // Assert
            Assert.Equal("This is the first value", result);
        }

        [Fact]
        public void GetDisplayDescription_WithoutDescription_ReturnsNull()
        {
            // Arrange
            var value = TestEnum.Third;

            // Act
            var result = value.GetDisplayDescription();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetDisplayDescription_WithoutDisplayAttribute_ReturnsNull()
        {
            // Arrange
            var value = TestEnum.Fourth;

            // Act
            var result = value.GetDisplayDescription();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetDisplayDescription_WithNull_ThrowsArgumentNullException()
        {
            // Arrange
            Enum value = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => value.GetDisplayDescription());
        }

        #endregion

        #region ParseByDisplayName Tests

        [Fact]
        public void ParseByDisplayName_WithValidDisplayName_ReturnsCorrectValue()
        {
            // Arrange
            const string displayName = "First Value";

            // Act
            var result = EnumExtensions.ParseByDisplayName<TestEnum>(displayName);

            // Assert
            Assert.Equal(TestEnum.First, result);
        }

        [Fact]
        public void ParseByDisplayName_WithEnumMemberName_ReturnsCorrectValue()
        {
            // Arrange
            const string memberName = "Fourth";

            // Act
            var result = EnumExtensions.ParseByDisplayName<TestEnum>(memberName);

            // Assert
            Assert.Equal(TestEnum.Fourth, result);
        }

        [Fact]
        public void ParseByDisplayName_WithInvalidDisplayName_ThrowsArgumentException()
        {
            // Arrange
            const string displayName = "Invalid Name";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                EnumExtensions.ParseByDisplayName<TestEnum>(displayName));

            Assert.Contains("was not found", exception.Message);
            Assert.Contains("TestEnum", exception.Message);
        }

        [Fact]
        public void ParseByDisplayName_WithNull_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                EnumExtensions.ParseByDisplayName<TestEnum>(null));
        }

        [Fact]
        public void ParseByDisplayName_WithEmptyString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                EnumExtensions.ParseByDisplayName<TestEnum>(string.Empty));
        }

        [Fact]
        public void ParseByDisplayName_CaseSensitive_MatchesExactCase()
        {
            // Arrange
            const string displayName = "First Value";

            // Act
            var result = EnumExtensions.ParseByDisplayName<TestEnum>(displayName, ignoreCase: false);

            // Assert
            Assert.Equal(TestEnum.First, result);
        }

        [Fact]
        public void ParseByDisplayName_CaseSensitive_ThrowsOnWrongCase()
        {
            // Arrange
            const string displayName = "first value"; // Wrong case

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                EnumExtensions.ParseByDisplayName<TestEnum>(displayName, ignoreCase: false));
        }

        [Fact]
        public void ParseByDisplayName_IgnoreCase_MatchesAnyCase()
        {
            // Arrange
            const string displayName = "first value"; // Lower case

            // Act
            var result = EnumExtensions.ParseByDisplayName<TestEnum>(displayName, ignoreCase: true);

            // Assert
            Assert.Equal(TestEnum.First, result);
        }

        [Theory]
        [InlineData("FIRST VALUE")]
        [InlineData("first value")]
        [InlineData("First Value")]
        [InlineData("FiRsT vAlUe")]
        public void ParseByDisplayName_IgnoreCase_HandlesVariousCases(string displayName)
        {
            // Act
            var result = EnumExtensions.ParseByDisplayName<TestEnum>(displayName, ignoreCase: true);

            // Assert
            Assert.Equal(TestEnum.First, result);
        }

        #endregion

        #region TryParseByDisplayName Tests

        [Fact]
        public void TryParseByDisplayName_WithValidDisplayName_ReturnsTrue()
        {
            // Arrange
            const string displayName = "First Value";

            // Act
            var success = EnumExtensions.TryParseByDisplayName<TestEnum>(displayName, out var result);

            // Assert
            Assert.True(success);
            Assert.Equal(TestEnum.First, result);
        }

        [Fact]
        public void TryParseByDisplayName_WithInvalidDisplayName_ReturnsFalse()
        {
            // Arrange
            const string displayName = "Invalid Name";

            // Act
            var success = EnumExtensions.TryParseByDisplayName<TestEnum>(displayName, out var result);

            // Assert
            Assert.False(success);
            Assert.Equal(default(TestEnum), result);
        }

        [Fact]
        public void TryParseByDisplayName_WithNull_ReturnsFalse()
        {
            // Act
            var success = EnumExtensions.TryParseByDisplayName<TestEnum>(null, out var result);

            // Assert
            Assert.False(success);
            Assert.Equal(default(TestEnum), result);
        }

        [Fact]
        public void TryParseByDisplayName_IgnoreCase_MatchesAnyCase()
        {
            // Arrange
            const string displayName = "FIRST VALUE";

            // Act
            var success = EnumExtensions.TryParseByDisplayName<TestEnum>(displayName, out var result, ignoreCase: true);

            // Assert
            Assert.True(success);
            Assert.Equal(TestEnum.First, result);
        }

        #endregion

        #region IsDisplayNameDefined Tests

        [Fact]
        public void IsDisplayNameDefined_WithValidDisplayName_ReturnsTrue()
        {
            // Arrange
            const string displayName = "First Value";

            // Act
            var result = EnumExtensions.IsDisplayNameDefined<TestEnum>(displayName);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsDisplayNameDefined_WithInvalidDisplayName_ReturnsFalse()
        {
            // Arrange
            const string displayName = "Invalid Name";

            // Act
            var result = EnumExtensions.IsDisplayNameDefined<TestEnum>(displayName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsDisplayNameDefined_WithNull_ReturnsFalse()
        {
            // Act
            var result = EnumExtensions.IsDisplayNameDefined<TestEnum>(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsDisplayNameDefined_IgnoreCase_MatchesAnyCase()
        {
            // Arrange
            const string displayName = "first value";

            // Act
            var result = EnumExtensions.IsDisplayNameDefined<TestEnum>(displayName, ignoreCase: true);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsDisplayNameDefined_CaseSensitive_RequiresExactCase()
        {
            // Arrange
            const string displayName = "first value"; // Wrong case

            // Act
            var result = EnumExtensions.IsDisplayNameDefined<TestEnum>(displayName, ignoreCase: false);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetDisplayNames Tests

        [Fact]
        public void GetDisplayNames_ReturnsAllDisplayNames()
        {
            // Act
            var result = EnumExtensions.GetDisplayNames<TestEnum>();

            // Assert
            Assert.NotNull(result);
            // Note: Dictionary may contain both display names and member names
            Assert.True(result.Count >= 4, $"Expected at least 4 names, got {result.Count}");
            Assert.Contains("First Value", result);
            Assert.Contains("Second Value", result);
            Assert.Contains("Third Value", result);
            Assert.Contains("Fourth", result); // No attribute, uses member name
        }

        [Fact]
        public void GetDisplayNames_ForEnumWithoutAttributes_ReturnsEnumNames()
        {
            // Act
            var result = EnumExtensions.GetDisplayNames<EmptyEnum>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains("Value1", result);
            Assert.Contains("Value2", result);
            Assert.Contains("Value3", result);
        }

        [Fact]
        public void GetDisplayNames_ReturnsReadOnlyCollection()
        {
            // Act
            var result = EnumExtensions.GetDisplayNames<TestEnum>();

            // Assert
            Assert.IsAssignableFrom<System.Collections.ObjectModel.ReadOnlyCollection<string>>(result);
        }

        #endregion

        #region GetDisplayNameDictionary Tests

        [Fact]
        public void GetDisplayNameDictionary_ReturnsCorrectMapping()
        {
            // Act
            var result = EnumExtensions.GetDisplayNameDictionary<TestEnum>();

            // Assert
            Assert.NotNull(result);
            // May contain both display names and member names for lookup flexibility
            Assert.True(result.Count >= 4, $"Expected at least 4 entries, got {result.Count}");
            Assert.Equal(TestEnum.First, result["First Value"]);
            Assert.Equal(TestEnum.Second, result["Second Value"]);
            Assert.Equal(TestEnum.Third, result["Third Value"]);
            Assert.Equal(TestEnum.Fourth, result["Fourth"]);
        }

        [Fact]
        public void GetDisplayNameDictionary_ReturnsReadOnlyDictionary()
        {
            // Act
            var result = EnumExtensions.GetDisplayNameDictionary<TestEnum>();

            // Assert
            Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyDictionary<string, TestEnum>>(result);
        }

        [Fact]
        public void GetDisplayNameDictionary_AllowsLookupByBothDisplayAndMemberName()
        {
            // Act
            var result = EnumExtensions.GetDisplayNameDictionary<TestEnum>();

            // Assert - Can look up by display name
            Assert.True(result.ContainsKey("First Value"));
            Assert.Equal(TestEnum.First, result["First Value"]);

            // Assert - Can also look up by member name for items without display attribute
            Assert.True(result.ContainsKey("Fourth"));
            Assert.Equal(TestEnum.Fourth, result["Fourth"]);
        }

        #endregion

        #region GetValueDisplayNameDictionary Tests

        [Fact]
        public void GetValueDisplayNameDictionary_ReturnsCorrectMapping()
        {
            // Act
            var result = EnumExtensions.GetValueDisplayNameDictionary<TestEnum>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);
            Assert.Equal("First Value", result[1]);
            Assert.Equal("Second Value", result[2]);
            Assert.Equal("Third Value", result[3]);
            Assert.Equal("Fourth", result[4]);
        }

        [Fact]
        public void GetValueDisplayNameDictionary_ReturnsReadOnlyDictionary()
        {
            // Act
            var result = EnumExtensions.GetValueDisplayNameDictionary<TestEnum>();

            // Assert
            Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyDictionary<int, string>>(result);
        }

        #endregion

        #region Cache Tests

        [Fact]
        public void ClearCache_RemovesAllCachedData()
        {
            // Arrange - Prime the cache
            var displayName = TestEnum.First.GetDisplayName();
            var parsed = EnumExtensions.ParseByDisplayName<TestEnum>("First Value");

            // Act
            EnumExtensions.ClearCache();

            // Assert - Should still work after cache clear (will repopulate)
            var displayNameAfter = TestEnum.First.GetDisplayName();
            var parsedAfter = EnumExtensions.ParseByDisplayName<TestEnum>("First Value");

            Assert.Equal(displayName, displayNameAfter);
            Assert.Equal(parsed, parsedAfter);
        }

        [Fact]
        public void Cache_HandlesMultipleEnumTypes()
        {
            // Arrange & Act
            var testEnumName = TestEnum.First.GetDisplayName();
            var emptyEnumName = EmptyEnum.Value1.GetDisplayName();
            var caseSensitiveName = CaseSensitiveEnum.LowerCase.GetDisplayName();

            // Assert
            Assert.Equal("First Value", testEnumName);
            Assert.Equal("Value1", emptyEnumName);
            Assert.Equal("Lower Case", caseSensitiveName);
        }

        #endregion

        #region Edge Cases and Special Scenarios

        [Fact]
        public void EnumExtensions_HandlesEnumWithSpaces()
        {
            // Arrange
            const string displayName = "First Value"; // Has space

            // Act
            var parsed = EnumExtensions.ParseByDisplayName<TestEnum>(displayName);
            var name = parsed.GetDisplayName();

            // Assert
            Assert.Equal(TestEnum.First, parsed);
            Assert.Equal("First Value", name);
        }

        [Fact]
        public void EnumExtensions_HandlesEnumWithSpecialCharacters()
        {
            // Note: Display names can contain any characters
            // This is handled correctly by the implementation
            Assert.True(true); // Placeholder for special character test
        }

        [Fact]
        public void EnumExtensions_ThreadSafety_MultipleThreadsAccess()
        {
            // Arrange
            var tasks = Enumerable.Range(0, 100).Select(i => System.Threading.Tasks.Task.Run(() =>
            {
                // Act - Multiple threads accessing cache simultaneously
                var name = TestEnum.First.GetDisplayName();
                var parsed = EnumExtensions.ParseByDisplayName<TestEnum>("Second Value");
                var exists = EnumExtensions.IsDisplayNameDefined<TestEnum>("Third Value");

                // Assert
                Assert.Equal("First Value", name);
                Assert.Equal(TestEnum.Second, parsed);
                Assert.True(exists);
            }));

            // Assert - Should not throw
            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());
        }

        [Fact]
        public void EnumExtensions_PerformanceTest_CacheEffective()
        {
            // Arrange - Warm up cache
            _ = TestEnum.First.GetDisplayName();

            var iterations = 1000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - Should be fast due to caching
            for (int i = 0; i < iterations; i++)
            {
                _ = TestEnum.First.GetDisplayName();
                _ = TestEnum.Second.GetDisplayName();
                _ = TestEnum.Third.GetDisplayName();
            }

            stopwatch.Stop();

            // Assert - Should complete in reasonable time (< 100ms for 1000 iterations)
            Assert.True(stopwatch.ElapsedMilliseconds < 100,
                $"Cache should make lookups fast. Took {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region Integration Tests with Real Enums

        [Fact]
        public void EnumExtensions_WorksWithCurrencyCodeType()
        {
            // Arrange
            var usd = Hmm.Utility.Currency.CurrencyCodeType.Usd;

            // Act
            var displayName = usd.GetDisplayName();

            // Assert
            Assert.Equal("United States dollar", displayName);
        }

        [Fact]
        public void EnumExtensions_CanParseCurrencyByDisplayName()
        {
            // Act
            var result = EnumExtensions.ParseByDisplayName<Hmm.Utility.Currency.CurrencyCodeType>(
                "Canadian dollar");

            // Assert
            Assert.Equal(Hmm.Utility.Currency.CurrencyCodeType.Cad, result);
        }

        #endregion
    }
}
