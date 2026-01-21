using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerialize;
using Hmm.Core.Map.DomainEntity;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class AutomobileJsonNoteSerializeTests : AutoTestFixtureBase
    {
        private AutomobileJsonNoteSerialize _serializer;
        private Author _author;

        public AutomobileJsonNoteSerializeTests()
        {
            SetupTestEnv();
        }

        #region GetNoteSerializationText Tests

        [Fact]
        public void GetNoteSerializationText_ValidEntity_ReturnsValidJson()
        {
            // Arrange
            var auto = CreateValidAutomobile();

            // Act
            var json = _serializer.GetNoteSerializationText(auto);

            // Assert
            Assert.NotEmpty(json);
            var document = JsonDocument.Parse(json);
            Assert.True(document.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
            Assert.True(contentElement.TryGetProperty(AutomobileConstant.AutoMobileRecordSubject, out _));
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
        public void GetNoteSerializationText_ContainsCoreIdentificationFields()
        {
            // Arrange
            var auto = CreateValidAutomobile();

            // Act
            var json = _serializer.GetNoteSerializationText(auto);

            // Assert
            Assert.Contains("\"vin\":", json);
            Assert.Contains("\"maker\":", json);
            Assert.Contains("\"brand\":", json);
            Assert.Contains("\"model\":", json);
            Assert.Contains("\"year\":", json);
            Assert.Contains("\"color\":", json);
            Assert.Contains("\"plate\":", json);
        }

        [Fact]
        public void GetNoteSerializationText_ContainsFuelAndEngineFields()
        {
            // Arrange
            var auto = CreateValidAutomobile();

            // Act
            var json = _serializer.GetNoteSerializationText(auto);

            // Assert
            Assert.Contains("\"engineType\":", json);
            Assert.Contains("\"fuelType\":", json);
            Assert.Contains("\"fuelTankCapacity\":", json);
            Assert.Contains("\"cityMPG\":", json);
            Assert.Contains("\"highwayMPG\":", json);
            Assert.Contains("\"combinedMPG\":", json);
        }

        [Fact]
        public void GetNoteSerializationText_ContainsOdometerField()
        {
            // Arrange
            var auto = CreateValidAutomobile();

            // Act
            var json = _serializer.GetNoteSerializationText(auto);

            // Assert
            Assert.Contains("\"meterReading\":", json);
        }

        [Fact]
        public void GetNoteSerializationText_WithOptionalFields_IncludesThemInJson()
        {
            // Arrange
            var auto = CreateValidAutomobile();
            auto.PurchaseDate = new DateTime(2020, 1, 15);
            auto.PurchasePrice = 35000m;
            auto.PurchaseMeterReading = 100;
            auto.InsuranceProvider = "Test Insurance Co";
            auto.InsurancePolicyNumber = "POL123456";

            // Act
            var json = _serializer.GetNoteSerializationText(auto);

            // Assert
            Assert.Contains("\"purchaseDate\":", json);
            Assert.Contains("\"purchasePrice\":", json);
            Assert.Contains("\"purchaseMeterReading\":", json);
            Assert.Contains("\"insuranceProvider\":", json);
            Assert.Contains("\"insurancePolicyNumber\":", json);
        }

        [Fact]
        public void GetNoteSerializationText_WithoutOptionalFields_ExcludesThemFromJson()
        {
            // Arrange
            var auto = CreateValidAutomobile();
            auto.PurchaseDate = null;
            auto.PurchasePrice = null;
            auto.InsuranceProvider = null;

            // Act
            var json = _serializer.GetNoteSerializationText(auto);

            // Assert
            Assert.DoesNotContain("\"purchaseDate\":", json);
            Assert.DoesNotContain("\"purchasePrice\":", json);
            Assert.DoesNotContain("\"insuranceProvider\":", json);
        }

        [Fact]
        public void GetNoteSerializationText_HandlesSpecialCharacters()
        {
            // Arrange
            var auto = CreateValidAutomobile();
            auto.Notes = "Test with \"quotes\" and <brackets>";

            // Act
            var json = _serializer.GetNoteSerializationText(auto);

            // Assert
            Assert.NotEmpty(json);
            // JSON should properly escape the special characters
            var document = JsonDocument.Parse(json);
            Assert.NotNull(document);
        }

        [Fact]
        public void GetNoteSerializationText_SerializesEnumsAsStrings()
        {
            // Arrange
            var auto = CreateValidAutomobile();
            auto.EngineType = FuelEngineType.Hybrid;
            auto.FuelType = FuelGrade.Premium;
            auto.OwnershipStatus = OwnershipType.Leased;

            // Act
            var json = _serializer.GetNoteSerializationText(auto);

            // Assert
            Assert.Contains("\"Hybrid\"", json);
            Assert.Contains("\"Premium\"", json);
            Assert.Contains("\"Leased\"", json);
        }

        [Fact]
        public void GetNoteSerializationText_SerializesDateTimesInIsoFormat()
        {
            // Arrange
            var auto = CreateValidAutomobile();
            auto.CreatedDate = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

            // Act
            var json = _serializer.GetNoteSerializationText(auto);

            // Assert
            Assert.Contains("2024-06-15T10:30:00", json);
        }

        #endregion GetNoteSerializationText Tests

        #region GetEntity Tests

        [Fact]
        public async Task GetEntity_ValidNote_ReturnsAutomobile()
        {
            // Arrange
            var auto = CreateValidAutomobile();
            var json = _serializer.GetNoteSerializationText(auto);
            var note = new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = AutomobileConstant.AutoMobileRecordSubject,
                Content = json,
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(auto.VIN, result.Value.VIN);
            Assert.Equal(auto.Maker, result.Value.Maker);
            Assert.Equal(auto.Brand, result.Value.Brand);
            Assert.Equal(auto.Year, result.Value.Year);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesCoreIdentificationFields()
        {
            // Arrange
            var auto = CreateValidAutomobile();
            auto.VIN = "1HGBH41JXMN109186";
            auto.Maker = "Honda";
            auto.Brand = "Accord";
            auto.Model = "Sedan";
            auto.Trim = "EX-L";
            auto.Year = 2022;
            auto.Color = "Silver";
            auto.Plate = "ABC1234";

            var json = _serializer.GetNoteSerializationText(auto);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("1HGBH41JXMN109186", result.Value.VIN);
            Assert.Equal("Honda", result.Value.Maker);
            Assert.Equal("Accord", result.Value.Brand);
            Assert.Equal("Sedan", result.Value.Model);
            Assert.Equal("EX-L", result.Value.Trim);
            Assert.Equal(2022, result.Value.Year);
            Assert.Equal("Silver", result.Value.Color);
            Assert.Equal("ABC1234", result.Value.Plate);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesFuelAndEngineFields()
        {
            // Arrange
            var auto = CreateValidAutomobile();
            auto.EngineType = FuelEngineType.Hybrid;
            auto.FuelType = FuelGrade.Regular;
            auto.FuelTankCapacity = 60.5m;
            auto.CityMPG = 25.5m;
            auto.HighwayMPG = 35.5m;
            auto.CombinedMPG = 30.0m;

            var json = _serializer.GetNoteSerializationText(auto);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(FuelEngineType.Hybrid, result.Value.EngineType);
            Assert.Equal(FuelGrade.Regular, result.Value.FuelType);
            Assert.Equal(60.5m, result.Value.FuelTankCapacity);
            Assert.Equal(25.5m, result.Value.CityMPG);
            Assert.Equal(35.5m, result.Value.HighwayMPG);
            Assert.Equal(30.0m, result.Value.CombinedMPG);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesOwnershipFields()
        {
            // Arrange
            var auto = CreateValidAutomobile();
            auto.PurchaseDate = new DateTime(2020, 6, 15, 0, 0, 0, DateTimeKind.Utc);
            auto.PurchasePrice = 35000m;
            auto.PurchaseMeterReading = 50;
            auto.OwnershipStatus = OwnershipType.Financed;

            var json = _serializer.GetNoteSerializationText(auto);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value.PurchaseDate);
            Assert.Equal(new DateTime(2020, 6, 15, 0, 0, 0, DateTimeKind.Utc), result.Value.PurchaseDate.Value);
            Assert.Equal(35000m, result.Value.PurchasePrice);
            Assert.Equal(50, result.Value.PurchaseMeterReading);
            Assert.Equal(OwnershipType.Financed, result.Value.OwnershipStatus);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesStatusFields()
        {
            // Arrange
            var auto = CreateValidAutomobile();
            auto.IsActive = false;
            auto.SoldDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            auto.SoldMeterReading = 100000;
            auto.SoldPrice = 20000m;

            var json = _serializer.GetNoteSerializationText(auto);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.False(result.Value.IsActive);
            Assert.NotNull(result.Value.SoldDate);
            Assert.Equal(100000, result.Value.SoldMeterReading);
            Assert.Equal(20000m, result.Value.SoldPrice);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesInsuranceFields()
        {
            // Arrange
            var auto = CreateValidAutomobile();
            auto.InsuranceExpiryDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
            auto.InsuranceProvider = "Test Insurance Co";
            auto.InsurancePolicyNumber = "POL123456";

            var json = _serializer.GetNoteSerializationText(auto);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value.InsuranceExpiryDate);
            Assert.Equal("Test Insurance Co", result.Value.InsuranceProvider);
            Assert.Equal("POL123456", result.Value.InsurancePolicyNumber);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesMaintenanceFields()
        {
            // Arrange
            var auto = CreateValidAutomobile();
            auto.LastServiceDate = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc);
            auto.LastServiceMeterReading = 45000;
            auto.NextServiceDueDate = new DateTime(2024, 9, 15, 0, 0, 0, DateTimeKind.Utc);
            auto.NextServiceDueMeterReading = 50000;

            var json = _serializer.GetNoteSerializationText(auto);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value.LastServiceDate);
            Assert.Equal(45000, result.Value.LastServiceMeterReading);
            Assert.NotNull(result.Value.NextServiceDueDate);
            Assert.Equal(50000, result.Value.NextServiceDueMeterReading);
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
                Subject = AutomobileConstant.AutoMobileRecordSubject,
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
                Subject = AutomobileConstant.AutoMobileRecordSubject,
                Content = "Not valid JSON content",
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
                Subject = AutomobileConstant.AutoMobileRecordSubject,
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
        public async Task GetEntity_MissingContentElement_ReturnsError()
        {
            // Arrange
            var note = new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = AutomobileConstant.AutoMobileRecordSubject,
                Content = "{\"note\": {}}",
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetEntity_MissingAutomobileElement_ReturnsError()
        {
            // Arrange
            var note = new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = AutomobileConstant.AutoMobileRecordSubject,
                Content = "{\"note\": {\"content\": {}}}",
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetEntity_SetsIdFromNote()
        {
            // Arrange
            var auto = CreateValidAutomobile();
            var json = _serializer.GetNoteSerializationText(auto);
            var note = new HmmNote
            {
                Id = 42,
                Author = _author,
                Subject = AutomobileConstant.AutoMobileRecordSubject,
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
            var auto = CreateValidAutomobile();
            var json = _serializer.GetNoteSerializationText(auto);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(_author.Id, result.Value.AuthorId);
        }

        #endregion GetEntity Tests

        #region GetNote Tests

        [Fact]
        public async Task GetNote_ValidEntity_ReturnsNote()
        {
            // Arrange
            var auto = CreateValidAutomobile();

            // Act
            var result = await _serializer.GetNote(auto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(AutomobileConstant.AutoMobileRecordSubject, result.Value.Subject);
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
            var auto = CreateValidAutomobile(123);

            // Act
            var result = await _serializer.GetNote(auto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(123, result.Value.Id);
        }

        [Fact]
        public async Task GetNote_SetsCorrectSubject()
        {
            // Arrange
            var auto = CreateValidAutomobile();

            // Act
            var result = await _serializer.GetNote(auto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(AutomobileConstant.AutoMobileRecordSubject, result.Value.Subject);
        }

        #endregion GetNote Tests

        #region Round-Trip Tests

        [Fact]
        public async Task RoundTrip_BasicFields_PreservesData()
        {
            // Arrange
            var original = CreateValidAutomobile();

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(original.VIN, result.Value.VIN);
            Assert.Equal(original.Maker, result.Value.Maker);
            Assert.Equal(original.Brand, result.Value.Brand);
            Assert.Equal(original.Model, result.Value.Model);
            Assert.Equal(original.Year, result.Value.Year);
            Assert.Equal(original.Color, result.Value.Color);
            Assert.Equal(original.Plate, result.Value.Plate);
        }

        [Fact]
        public async Task RoundTrip_AllEnumTypes_PreservesData()
        {
            // Arrange
            var original = CreateValidAutomobile();
            original.EngineType = FuelEngineType.Electric;
            original.FuelType = FuelGrade.E85;
            original.OwnershipStatus = OwnershipType.Company;

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(FuelEngineType.Electric, result.Value.EngineType);
            Assert.Equal(FuelGrade.E85, result.Value.FuelType);
            Assert.Equal(OwnershipType.Company, result.Value.OwnershipStatus);
        }

        [Fact]
        public async Task RoundTrip_AllOptionalFields_PreservesData()
        {
            // Arrange
            var original = CreateValidAutomobile();
            original.PurchaseDate = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            original.PurchasePrice = 35000.50m;
            original.PurchaseMeterReading = 100;
            original.SoldDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            original.SoldMeterReading = 95000;
            original.SoldPrice = 22000m;
            original.RegistrationExpiryDate = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc);
            original.InsuranceExpiryDate = new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc);
            original.InsuranceProvider = "Test Insurance";
            original.InsurancePolicyNumber = "POL999";
            original.LastServiceDate = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc);
            original.LastServiceMeterReading = 90000;
            original.NextServiceDueDate = new DateTime(2024, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            original.NextServiceDueMeterReading = 95000;
            original.Notes = "Test notes with special chars: <>&\"'";

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(original.PurchaseDate, result.Value.PurchaseDate);
            Assert.Equal(original.PurchasePrice, result.Value.PurchasePrice);
            Assert.Equal(original.PurchaseMeterReading, result.Value.PurchaseMeterReading);
            Assert.Equal(original.SoldDate, result.Value.SoldDate);
            Assert.Equal(original.SoldMeterReading, result.Value.SoldMeterReading);
            Assert.Equal(original.SoldPrice, result.Value.SoldPrice);
            Assert.Equal(original.InsuranceProvider, result.Value.InsuranceProvider);
            Assert.Equal(original.InsurancePolicyNumber, result.Value.InsurancePolicyNumber);
            Assert.Equal(original.Notes, result.Value.Notes);
        }

        [Fact]
        public async Task RoundTrip_NumericFields_PreservesData()
        {
            // Arrange
            var original = CreateValidAutomobile();
            original.MeterReading = 123456789;
            original.FuelTankCapacity = 75.5m;
            original.CityMPG = 28.3m;
            original.HighwayMPG = 38.7m;
            original.CombinedMPG = 32.1m;

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(123456789, result.Value.MeterReading);
            Assert.Equal(75.5m, result.Value.FuelTankCapacity);
            Assert.Equal(28.3m, result.Value.CityMPG);
            Assert.Equal(38.7m, result.Value.HighwayMPG);
            Assert.Equal(32.1m, result.Value.CombinedMPG);
        }

        [Fact]
        public async Task RoundTrip_IsActiveFlag_PreservesData()
        {
            // Arrange
            var original = CreateValidAutomobile();
            original.IsActive = false;

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.False(result.Value.IsActive);
        }

        #endregion Round-Trip Tests

        #region Helper Methods

        private AutomobileInfo CreateValidAutomobile(int id = 0)
        {
            var autoId = id > 0 ? id : 1;
            return new AutomobileInfo
            {
                Id = autoId,
                AuthorId = _author.Id,
                VIN = "1HGBH41JXMN109186",
                Maker = "Subaru",
                Brand = "Outback",
                Model = "Limited",
                Trim = "3.6R",
                Year = 2018,
                Color = "Blue",
                Plate = "BCTT208",
                EngineType = FuelEngineType.Gasoline,
                FuelType = FuelGrade.Regular,
                FuelTankCapacity = 70.0m,
                CityMPG = 25.0m,
                HighwayMPG = 32.0m,
                CombinedMPG = 28.0m,
                MeterReading = 50000,
                OwnershipStatus = OwnershipType.Owned,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        private HmmNote CreateNote(string content)
        {
            return new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = AutomobileConstant.AutoMobileRecordSubject,
                Content = content,
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _serializer = new AutomobileJsonNoteSerialize(Application, new NullLogger<AutomobileInfo>(), LookupRepository);
            _author = TestDefaultAuthor;
        }

        #endregion Helper Methods
    }
}