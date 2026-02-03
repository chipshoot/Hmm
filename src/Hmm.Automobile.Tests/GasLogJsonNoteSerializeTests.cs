using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerialize;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Currency;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.MeasureUnit;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class GasLogJsonNoteSerializeTests : AutoTestFixtureBase
    {
        private GasLogJsonNoteSerialize _serializer;
        private Author _author;
        private AutomobileInfo _testCar;
        private GasStation _testStation;
        private GasDiscount _testDiscount;
        private Mock<IAutoEntityManager<AutomobileInfo>> _autoManagerMock;
        private Mock<IAutoEntityManager<GasDiscount>> _discountManagerMock;
        private Mock<IAutoEntityManager<GasStation>> _stationManagerMock;

        public GasLogJsonNoteSerializeTests()
        {
            SetupTestEnv();
        }

        #region GetNoteSerializationText Tests

        [Fact]
        public void GetNoteSerializationText_ValidEntity_ReturnsValidJson()
        {
            // Arrange
            var gasLog = CreateValidGasLog();

            // Act
            var json = _serializer.GetNoteSerializationText(gasLog);

            // Assert
            Assert.NotEmpty(json);
            var document = JsonDocument.Parse(json);
            Assert.True(document.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
            Assert.True(contentElement.TryGetProperty(AutomobileConstant.GasLogRecordSubject, out _));
        }

        [Fact]
        public void GetNoteSerializationText_NullEntity_ReturnsEmptyString()
        {
            // Arrange & Act
            var json = _serializer.GetNoteSerializationText(null);

            // Assert
            Assert.Empty(json);
        }

        [Fact]
        public void GetNoteSerializationText_ContainsCoreFields()
        {
            // Arrange
            var gasLog = CreateValidGasLog();

            // Act
            var json = _serializer.GetNoteSerializationText(gasLog);

            // Assert
            Assert.Contains("\"date\":", json);
            Assert.Contains("\"automobile\":", json);
            Assert.Contains("\"distance\":", json);
            Assert.Contains("\"odometer\":", json);
            Assert.Contains("\"fuel\":", json);
            Assert.Contains("\"totalPrice\":", json);
            Assert.Contains("\"fuelGrade\":", json);
        }

        [Fact]
        public void GetNoteSerializationText_SerializesDateInIsoFormat()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.Date = new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Utc);

            // Act
            var json = _serializer.GetNoteSerializationText(gasLog);

            // Assert
            Assert.Contains("2024-06-15", json);
        }

        [Fact]
        public void GetNoteSerializationText_SerializesFuelGradeAsString()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.FuelGrade = FuelGrade.Premium;

            // Act
            var json = _serializer.GetNoteSerializationText(gasLog);

            // Assert
            Assert.Contains("\"Premium\"", json);
        }

        [Theory]
        [InlineData(FuelGrade.Regular)]
        [InlineData(FuelGrade.Premium)]
        [InlineData(FuelGrade.E85)]
        [InlineData(FuelGrade.Diesel)]
        public void GetNoteSerializationText_SerializesAllFuelGrades(FuelGrade fuelGrade)
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.FuelGrade = fuelGrade;

            // Act
            var json = _serializer.GetNoteSerializationText(gasLog);

            // Assert
            Assert.Contains($"\"{fuelGrade}\"", json);
        }

        [Fact]
        public void GetNoteSerializationText_IncludesStationWhenSet()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.Station = _testStation;

            // Act
            var json = _serializer.GetNoteSerializationText(gasLog);

            // Assert
            Assert.Contains("\"station\":", json);
        }

        [Fact]
        public void GetNoteSerializationText_IncludesDiscountsArray()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.Discounts = new List<GasDiscountInfo>
            {
                new GasDiscountInfo
                {
                    Program = _testDiscount,
                    Amount = new Money(5.00m, CurrencyCodeType.Cad)
                }
            };

            // Act
            var json = _serializer.GetNoteSerializationText(gasLog);

            // Assert
            Assert.Contains("\"discounts\":", json);
        }

        [Fact]
        public void GetNoteSerializationText_EmptyDiscountsArray()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.Discounts = new List<GasDiscountInfo>();

            // Act
            var json = _serializer.GetNoteSerializationText(gasLog);

            // Assert
            Assert.Contains("\"discounts\":[]", json);
        }

        [Fact]
        public void GetNoteSerializationText_DiscountWithNullAmount_ReturnsEmptyString()
        {
            // Arrange - Issue #56 fix: serialization should fail fast on invalid data
            var gasLog = CreateValidGasLog();
            gasLog.Discounts = new List<GasDiscountInfo>
            {
                new GasDiscountInfo
                {
                    Program = _testDiscount,
                    Amount = null // Invalid: Amount is required
                }
            };

            // Act
            var json = _serializer.GetNoteSerializationText(gasLog);

            // Assert - Serialization should fail (return empty string) rather than silently lose data
            Assert.Empty(json);
        }

        [Fact]
        public void GetNoteSerializationText_DiscountWithNullProgram_ReturnsEmptyString()
        {
            // Arrange - Issue #56 fix: serialization should fail fast on invalid data
            var gasLog = CreateValidGasLog();
            gasLog.Discounts = new List<GasDiscountInfo>
            {
                new GasDiscountInfo
                {
                    Program = null, // Invalid: Program is required
                    Amount = new Money(5.00m, CurrencyCodeType.Cad)
                }
            };

            // Act
            var json = _serializer.GetNoteSerializationText(gasLog);

            // Assert - Serialization should fail (return empty string) rather than silently lose data
            Assert.Empty(json);
        }

        [Fact]
        public void GetNoteSerializationText_MultipleDiscounts_OneNullAmount_ReturnsEmptyString()
        {
            // Arrange - Issue #56 fix: fail fast even when only one discount is invalid
            var gasLog = CreateValidGasLog();
            gasLog.Discounts = new List<GasDiscountInfo>
            {
                new GasDiscountInfo
                {
                    Program = _testDiscount,
                    Amount = new Money(5.00m, CurrencyCodeType.Cad) // Valid
                },
                new GasDiscountInfo
                {
                    Program = _testDiscount,
                    Amount = null // Invalid
                }
            };

            // Act
            var json = _serializer.GetNoteSerializationText(gasLog);

            // Assert - Entire serialization should fail, not just skip the invalid discount
            Assert.Empty(json);
        }

        [Fact]
        public async Task GetNote_DiscountWithNullAmount_ReturnsError()
        {
            // Arrange - Issue #56 fix: GetNote should return failure when serialization fails
            var gasLog = CreateValidGasLog();
            gasLog.Discounts = new List<GasDiscountInfo>
            {
                new GasDiscountInfo
                {
                    Program = _testDiscount,
                    Amount = null // Invalid
                }
            };

            // Act
            var result = await _serializer.GetNote(gasLog);

            // Assert - Should fail because serialization fails
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetNote_DiscountWithNullProgram_ReturnsError()
        {
            // Arrange - Issue #56 fix: GetNote should return failure when serialization fails
            var gasLog = CreateValidGasLog();
            gasLog.Discounts = new List<GasDiscountInfo>
            {
                new GasDiscountInfo
                {
                    Program = null, // Invalid
                    Amount = new Money(5.00m, CurrencyCodeType.Cad)
                }
            };

            // Act
            var result = await _serializer.GetNote(gasLog);

            // Assert - Should fail because serialization fails
            Assert.False(result.Success);
        }

        [Fact]
        public void GetNoteSerializationText_IncludesOptionalDrivingContext()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.CityDrivingPercentage = 60;
            gasLog.HighwayDrivingPercentage = 40;

            // Act
            var json = _serializer.GetNoteSerializationText(gasLog);

            // Assert
            Assert.Contains("\"cityDrivingPercentage\":60", json);
            Assert.Contains("\"highwayDrivingPercentage\":40", json);
        }

        [Fact]
        public void GetNoteSerializationText_IncludesLocation()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.Location = "Vancouver, BC";

            // Act
            var json = _serializer.GetNoteSerializationText(gasLog);

            // Assert
            Assert.Contains("\"location\":\"Vancouver, BC\"", json);
        }

        [Fact]
        public void GetNoteSerializationText_HandlesComment()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.Comment = "Regular fill-up";

            // Act
            var json = _serializer.GetNoteSerializationText(gasLog);

            // Assert
            Assert.Contains("\"comment\":\"Regular fill-up\"", json);
        }

        [Fact]
        public void GetNoteSerializationText_HandlesSpecialCharactersInComment()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.Comment = "Test with \"quotes\" and <brackets>";

            // Act
            var json = _serializer.GetNoteSerializationText(gasLog);

            // Assert
            Assert.NotEmpty(json);
            var document = JsonDocument.Parse(json);
            Assert.NotNull(document);
        }

        #endregion

        #region GetEntity Tests

        [Fact]
        public async Task GetEntity_ValidNote_ReturnsGasLog()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            var json = _serializer.GetNoteSerializationText(gasLog);
            var note = CreateNote(json, gasLog.AutomobileId);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(gasLog.AutomobileId, result.Value.AutomobileId);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesDateField()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.Date = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
            var json = _serializer.GetNoteSerializationText(gasLog);
            var note = CreateNote(json, gasLog.AutomobileId);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2024, result.Value.Date.Year);
            Assert.Equal(6, result.Value.Date.Month);
            Assert.Equal(15, result.Value.Date.Day);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ResolvesAutomobile()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            var json = _serializer.GetNoteSerializationText(gasLog);
            var note = CreateNote(json, gasLog.AutomobileId);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value.Car);
            Assert.Equal(_testCar.Id, result.Value.Car.Id);
            Assert.Equal(_testCar.Maker, result.Value.Car.Maker);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesFuelGrade()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.FuelGrade = FuelGrade.Premium;
            var json = _serializer.GetNoteSerializationText(gasLog);
            var note = CreateNote(json, gasLog.AutomobileId);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(FuelGrade.Premium, result.Value.FuelGrade);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesBooleanFlags()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.IsFullTank = false;
            gasLog.IsFirstFillUp = true;
            var json = _serializer.GetNoteSerializationText(gasLog);
            var note = CreateNote(json, gasLog.AutomobileId);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.False(result.Value.IsFullTank);
            Assert.True(result.Value.IsFirstFillUp);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesLocation()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.Location = "Downtown Vancouver";
            var json = _serializer.GetNoteSerializationText(gasLog);
            var note = CreateNote(json, gasLog.AutomobileId);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Downtown Vancouver", result.Value.Location);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesComment()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.Comment = "Test comment for gas log";
            var json = _serializer.GetNoteSerializationText(gasLog);
            var note = CreateNote(json, gasLog.AutomobileId);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Test comment for gas log", result.Value.Comment);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ResolvesStationById()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.Station = _testStation;
            var json = _serializer.GetNoteSerializationText(gasLog);
            var note = CreateNote(json, gasLog.AutomobileId);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value.Station);
            Assert.Equal(_testStation.Id, result.Value.Station.Id);
            Assert.Equal(_testStation.Name, result.Value.Station.Name);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesDiscounts()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.Discounts = new List<GasDiscountInfo>
            {
                new GasDiscountInfo
                {
                    Program = _testDiscount,
                    Amount = new Money(3.50m, CurrencyCodeType.Cad)
                }
            };
            var json = _serializer.GetNoteSerializationText(gasLog);
            var note = CreateNote(json, gasLog.AutomobileId);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value.Discounts);
            Assert.Single(result.Value.Discounts);
            Assert.Equal(_testDiscount.Id, result.Value.Discounts[0].Program.Id);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesMultipleDiscounts_UsingBatchRetrieval()
        {
            // Arrange - This test verifies Issue #34 fix: batch retrieval instead of N+1 queries
            var discount2 = new GasDiscount
            {
                Id = 2,
                AuthorId = _author.Id,
                Program = "AirMiles",
                Amount = new Money(0.05m, CurrencyCodeType.Cad),
                DiscountType = GasDiscountType.PerLiter,
                IsActive = true
            };
            var discount3 = new GasDiscount
            {
                Id = 3,
                AuthorId = _author.Id,
                Program = "CIBC Rewards",
                Amount = new Money(0.03m, CurrencyCodeType.Cad),
                DiscountType = GasDiscountType.PerLiter,
                IsActive = true
            };

            // Setup mock to return all discounts in a single batch query
            var allDiscounts = new PageList<GasDiscount>(
                new List<GasDiscount> { _testDiscount, discount2, discount3 }, 3, 1, 100);

            _discountManagerMock.Setup(m => m.GetEntitiesAsync(It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<GasDiscount>>.Ok(allDiscounts));

            var gasLog = CreateValidGasLog();
            gasLog.Discounts = new List<GasDiscountInfo>
            {
                new GasDiscountInfo { Program = _testDiscount, Amount = new Money(5.00m, CurrencyCodeType.Cad) },
                new GasDiscountInfo { Program = discount2, Amount = new Money(3.00m, CurrencyCodeType.Cad) },
                new GasDiscountInfo { Program = discount3, Amount = new Money(2.00m, CurrencyCodeType.Cad) }
            };

            var json = _serializer.GetNoteSerializationText(gasLog);
            var note = CreateNote(json, gasLog.AutomobileId);

            // Act - Should use batch retrieval (GetEntitiesAsync) instead of 3 individual queries
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value.Discounts);
            Assert.Equal(3, result.Value.Discounts.Count);
            Assert.Contains(result.Value.Discounts, d => d.Program.Id == _testDiscount.Id);
            Assert.Contains(result.Value.Discounts, d => d.Program.Id == discount2.Id);
            Assert.Contains(result.Value.Discounts, d => d.Program.Id == discount3.Id);

            // Verify batch retrieval was called (not individual GetEntityByIdAsync calls for each discount)
            _discountManagerMock.Verify(m => m.GetEntitiesAsync(It.IsAny<ResourceCollectionParameters>()), Times.Once);
        }

        [Fact]
        public async Task GetEntity_ValidNote_HandlesPartialDiscountMatches()
        {
            // Arrange - Test when some discounts don't exist in the database
            // Create a non-existent discount to include in the gas log
            var nonExistentDiscount = new GasDiscount
            {
                Id = 999, // This ID won't exist in the batch query result
                AuthorId = _author.Id,
                Program = "NonExistent",
                Amount = new Money(0.05m, CurrencyCodeType.Cad),
                DiscountType = GasDiscountType.PerLiter,
                IsActive = true
            };

            // Setup mock to return only the existing test discount (not the non-existent one)
            var partialDiscounts = new PageList<GasDiscount>(
                new List<GasDiscount> { _testDiscount }, 1, 1, 100);

            _discountManagerMock.Setup(m => m.GetEntitiesAsync(It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<GasDiscount>>.Ok(partialDiscounts));

            // Create gas log with both existing and non-existing discounts
            var gasLog = CreateValidGasLog();
            gasLog.Discounts = new List<GasDiscountInfo>
            {
                new GasDiscountInfo { Program = _testDiscount, Amount = new Money(5.00m, CurrencyCodeType.Cad) },
                new GasDiscountInfo { Program = nonExistentDiscount, Amount = new Money(3.00m, CurrencyCodeType.Cad) }
            };

            var json = _serializer.GetNoteSerializationText(gasLog);
            var note = CreateNote(json, gasLog.AutomobileId);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert - Should only include the discount that was found in batch query
            Assert.True(result.Success);
            Assert.NotNull(result.Value.Discounts);
            Assert.Single(result.Value.Discounts);
            Assert.Equal(_testDiscount.Id, result.Value.Discounts[0].Program.Id);
        }

        [Fact]
        public async Task GetEntity_ValidNote_HandlesEmptyDiscountsArray()
        {
            // Arrange
            var emptyDiscounts = new PageList<GasDiscount>(
                new List<GasDiscount>(), 0, 1, 100);

            _discountManagerMock.Setup(m => m.GetEntitiesAsync(It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<GasDiscount>>.Ok(emptyDiscounts));

            var gasLog = CreateValidGasLog();
            gasLog.Discounts = new List<GasDiscountInfo>(); // Empty discounts
            var json = _serializer.GetNoteSerializationText(gasLog);
            var note = CreateNote(json, gasLog.AutomobileId);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            // Discounts should be null or empty since none were in the JSON
            Assert.True(result.Value.Discounts == null || result.Value.Discounts.Count == 0);
        }

        [Fact]
        public async Task GetEntity_NullNote_ReturnsError()
        {
            // Arrange & Act
            var result = await _serializer.GetEntity(null);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetEntity_EmptyContent_ReturnsError()
        {
            // Arrange
            var note = new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = GasLog.GetNoteSubject(1),
                Content = "",
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetEntity_InvalidJson_ReturnsError()
        {
            // Arrange
            var note = new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = GasLog.GetNoteSubject(1),
                Content = "Not valid JSON",
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetEntity_MissingNoteElement_ReturnsError()
        {
            // Arrange
            var note = new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = GasLog.GetNoteSubject(1),
                Content = "{\"data\": {}}",
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetEntity_InvalidAutomobileId_ReturnsError()
        {
            // Arrange - set up manager to return not found for specific ID
            _autoManagerMock.Setup(m => m.GetEntityByIdAsync(999))
                .ReturnsAsync(ProcessingResult<AutomobileInfo>.NotFound("Not found"));

            var json = "{\"note\":{\"content\":{\"GasLog\":{\"date\":\"2024-01-01T00:00:00Z\",\"automobile\":999}}}}";
            var note = CreateNote(json, 999);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetEntity_SetsIdFromNote()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            var json = _serializer.GetNoteSerializationText(gasLog);
            var note = new HmmNote
            {
                Id = 42,
                Author = _author,
                Subject = GasLog.GetNoteSubject(gasLog.AutomobileId),
                Content = json,
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(42, result.Value.Id);
        }

        [Fact]
        public async Task GetEntity_SetsAuthorIdFromNote()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            var json = _serializer.GetNoteSerializationText(gasLog);
            var note = CreateNote(json, gasLog.AutomobileId);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(_author.Id, result.Value.AuthorId);
        }

        #endregion

        #region GetNote Tests

        [Fact]
        public async Task GetNote_ValidEntity_ReturnsNote()
        {
            // Arrange
            var gasLog = CreateValidGasLog();

            // Act
            var result = await _serializer.GetNote(gasLog);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.NotEmpty(result.Value.Content);
        }

        [Fact]
        public async Task GetNote_NullEntity_ReturnsError()
        {
            // Arrange & Act
            var result = await _serializer.GetNote(null);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetNote_SetsNoteIdFromEntity()
        {
            // Arrange
            var gasLog = new GasLog
            {
                Id = 123,
                AuthorId = _author.Id,
                Date = DateTime.UtcNow,
                AutomobileId = _testCar.Id,
                Car = _testCar,
                Distance = Dimension.FromKilometer(350),
                Odometer = Dimension.FromKilometer(50000),
                Fuel = Volume.FromLiter(45.5),
                TotalPrice = new Money(85.00m, CurrencyCodeType.Cad),
                UnitPrice = new Money(1.87m, CurrencyCodeType.Cad),
                FuelGrade = FuelGrade.Regular,
                IsFullTank = true,
                IsFirstFillUp = false,
                CreateDate = DateTime.UtcNow,
                Discounts = new List<GasDiscountInfo>()
            };

            // Act
            var result = await _serializer.GetNote(gasLog);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(123, result.Value.Id);
        }

        [Fact]
        public async Task GetNote_SetsCorrectSubjectWithAutomobileId()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.AutomobileId = 5;

            // Act
            var result = await _serializer.GetNote(gasLog);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(GasLog.GetNoteSubject(5), result.Value.Subject);
        }

        #endregion

        #region Round-Trip Tests

        [Fact]
        public async Task RoundTrip_CoreFields_PreservesData()
        {
            // Arrange
            var original = CreateValidGasLog();
            original.Date = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json, original.AutomobileId);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(original.AutomobileId, result.Value.AutomobileId);
            Assert.Equal(original.Date.Year, result.Value.Date.Year);
            Assert.Equal(original.Date.Month, result.Value.Date.Month);
            Assert.Equal(original.Date.Day, result.Value.Date.Day);
        }

        [Fact]
        public async Task RoundTrip_FuelGrade_PreservesData()
        {
            // Arrange
            var original = CreateValidGasLog();
            original.FuelGrade = FuelGrade.E85;

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json, original.AutomobileId);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(FuelGrade.E85, result.Value.FuelGrade);
        }

        [Fact]
        public async Task RoundTrip_BooleanFlags_PreservesData()
        {
            // Arrange
            var original = CreateValidGasLog();
            original.IsFullTank = false;
            original.IsFirstFillUp = true;

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json, original.AutomobileId);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.False(result.Value.IsFullTank);
            Assert.True(result.Value.IsFirstFillUp);
        }

        [Fact]
        public async Task RoundTrip_Location_PreservesData()
        {
            // Arrange
            var original = CreateValidGasLog();
            original.Location = "123 Main St, Vancouver, BC";

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json, original.AutomobileId);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("123 Main St, Vancouver, BC", result.Value.Location);
        }

        [Fact]
        public async Task RoundTrip_Comment_PreservesData()
        {
            // Arrange
            var original = CreateValidGasLog();
            original.Comment = "Regular fill-up with premium fuel";

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json, original.AutomobileId);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Regular fill-up with premium fuel", result.Value.Comment);
        }

        [Fact]
        public async Task RoundTrip_SpecialCharacters_PreservesData()
        {
            // Arrange
            var original = CreateValidGasLog();
            original.Comment = "Test with \"quotes\" and <brackets> & ampersand";
            original.Location = "Address with 'apostrophe' and \"quotes\"";

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json, original.AutomobileId);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(original.Comment, result.Value.Comment);
            Assert.Equal(original.Location, result.Value.Location);
        }

        [Fact]
        public async Task RoundTrip_Station_PreservesData()
        {
            // Arrange
            var original = CreateValidGasLog();
            original.Station = _testStation;

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json, original.AutomobileId);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value.Station);
            Assert.Equal(_testStation.Id, result.Value.Station.Id);
            Assert.Equal(_testStation.Name, result.Value.Station.Name);
        }

        [Fact]
        public async Task RoundTrip_Discounts_PreservesData()
        {
            // Arrange
            var original = CreateValidGasLog();
            original.Discounts = new List<GasDiscountInfo>
            {
                new GasDiscountInfo
                {
                    Program = _testDiscount,
                    Amount = new Money(2.50m, CurrencyCodeType.Cad)
                }
            };

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json, original.AutomobileId);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value.Discounts);
            Assert.Single(result.Value.Discounts);
            Assert.Equal(_testDiscount.Id, result.Value.Discounts[0].Program.Id);
        }

        [Fact]
        public async Task RoundTrip_AllFuelGrades_PreserveData()
        {
            foreach (var fuelGrade in new[] { FuelGrade.Regular, FuelGrade.Premium, FuelGrade.E85, FuelGrade.Diesel })
            {
                // Arrange
                var original = CreateValidGasLog();
                original.FuelGrade = fuelGrade;

                // Act
                var json = _serializer.GetNoteSerializationText(original);
                var note = CreateNote(json, original.AutomobileId);
                var result = await _serializer.GetEntity(note);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(fuelGrade, result.Value.FuelGrade);
            }
        }

        #endregion

        #region Helper Methods

        private GasLog CreateValidGasLog()
        {
            return new GasLog
            {
                Id = 1,
                AuthorId = _author.Id,
                Date = DateTime.UtcNow,
                AutomobileId = _testCar.Id,
                Car = _testCar,
                Distance = Dimension.FromKilometer(350),
                Odometer = Dimension.FromKilometer(50000),
                Fuel = Volume.FromLiter(45.5),
                TotalPrice = new Money(85.00m, CurrencyCodeType.Cad),
                UnitPrice = new Money(1.87m, CurrencyCodeType.Cad),
                FuelGrade = FuelGrade.Regular,
                IsFullTank = true,
                IsFirstFillUp = false,
                Comment = string.Empty,
                CreateDate = DateTime.UtcNow,
                Discounts = new List<GasDiscountInfo>()
            };
        }

        private HmmNote CreateNote(string content, int automobileId)
        {
            return new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = GasLog.GetNoteSubject(automobileId),
                Content = content,
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _author = TestDefaultAuthor;

            // Setup test entities
            _testCar = new AutomobileInfo
            {
                Id = 1,
                AuthorId = _author.Id,
                Maker = "Subaru",
                Brand = "Outback",
                Year = 2018,
                Plate = "BCTT208",
                IsActive = true
            };

            _testStation = new GasStation
            {
                Id = 1,
                AuthorId = _author.Id,
                Name = "Costco Gas",
                Address = "123 Main Street",
                City = "Vancouver",
                State = "BC",
                ZipCode = "V6B 1A1",
                IsActive = true
            };

            _testDiscount = new GasDiscount
            {
                Id = 1,
                AuthorId = _author.Id,
                Program = "Petro-Points",
                Amount = new Money(0.10m, CurrencyCodeType.Cad),
                DiscountType = GasDiscountType.PerLiter,
                IsActive = true
            };

            // Setup mocks
            _autoManagerMock = new Mock<IAutoEntityManager<AutomobileInfo>>();
            _autoManagerMock.Setup(m => m.GetEntityByIdAsync(_testCar.Id))
                .ReturnsAsync(ProcessingResult<AutomobileInfo>.Ok(_testCar));

            _discountManagerMock = new Mock<IAutoEntityManager<GasDiscount>>();
            _discountManagerMock.Setup(m => m.GetEntityByIdAsync(_testDiscount.Id))
                .ReturnsAsync(ProcessingResult<GasDiscount>.Ok(_testDiscount));
            // Setup batch retrieval for GetEntitiesAsync (used by discount parsing after Issue #34 fix)
            var discountsList = new PageList<GasDiscount>(
                new List<GasDiscount> { _testDiscount }, 1, 1, 100);
            _discountManagerMock.Setup(m => m.GetEntitiesAsync(It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<GasDiscount>>.Ok(discountsList));
            _discountManagerMock.Setup(m => m.GetEntitiesAsync())
                .ReturnsAsync(ProcessingResult<PageList<GasDiscount>>.Ok(discountsList));

            _stationManagerMock = new Mock<IAutoEntityManager<GasStation>>();
            _stationManagerMock.Setup(m => m.GetEntityByIdAsync(_testStation.Id))
                .ReturnsAsync(ProcessingResult<GasStation>.Ok(_testStation));

            // Create serializer with mocked dependencies
            _serializer = new GasLogJsonNoteSerialize(
                CatalogProvider,
                new NullLogger<GasLog>(),
                _autoManagerMock.Object,
                _discountManagerMock.Object,
                _stationManagerMock.Object);
        }

        #endregion
    }
}
