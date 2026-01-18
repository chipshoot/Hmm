using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerialize;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Currency;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class GasDiscountJsonNoteSerializeTests : AutoTestFixtureBase
    {
        private GasDiscountJsonNoteSerialize _serializer;
        private Author _author;

        public GasDiscountJsonNoteSerializeTests()
        {
            SetupTestEnv();
        }

        #region GetNoteSerializationText Tests

        [Fact]
        public void GetNoteSerializationText_ValidEntity_ReturnsValidJson()
        {
            // Arrange
            var discount = CreateValidDiscount();

            // Act
            var json = _serializer.GetNoteSerializationText(discount);

            // Assert
            Assert.NotEmpty(json);
            var document = JsonDocument.Parse(json);
            Assert.True(document.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
            Assert.True(contentElement.TryGetProperty(AutomobileConstant.GasDiscountRecordSubject, out _));
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
        public void GetNoteSerializationText_ContainsAllFields()
        {
            // Arrange
            var discount = CreateValidDiscount();

            // Act
            var json = _serializer.GetNoteSerializationText(discount);

            // Assert
            Assert.Contains("\"program\":", json);
            Assert.Contains("\"amount\":", json);
            Assert.Contains("\"discountType\":", json);
            Assert.Contains("\"isActive\":", json);
            Assert.Contains("\"comment\":", json);
        }

        [Fact]
        public void GetNoteSerializationText_SerializesDiscountTypeAsString()
        {
            // Arrange
            var discount = CreateValidDiscount();
            discount.DiscountType = GasDiscountType.PerLiter;

            // Act
            var json = _serializer.GetNoteSerializationText(discount);

            // Assert
            Assert.Contains("\"PerLiter\"", json);
        }

        [Theory]
        [InlineData(GasDiscountType.PerLiter)]
        [InlineData(GasDiscountType.Flat)]
        public void GetNoteSerializationText_SerializesAllDiscountTypes(GasDiscountType discountType)
        {
            // Arrange
            var discount = CreateValidDiscount();
            discount.DiscountType = discountType;

            // Act
            var json = _serializer.GetNoteSerializationText(discount);

            // Assert
            Assert.Contains($"\"{discountType}\"", json);
        }

        [Fact]
        public void GetNoteSerializationText_HandlesNullProgram()
        {
            // Arrange
            var discount = CreateValidDiscount();
            discount.Program = null;

            // Act
            var json = _serializer.GetNoteSerializationText(discount);

            // Assert
            Assert.NotEmpty(json);
            Assert.Contains("\"program\":\"\"", json);
        }

        [Fact]
        public void GetNoteSerializationText_HandlesNullComment()
        {
            // Arrange
            var discount = CreateValidDiscount();
            discount.Comment = null;

            // Act
            var json = _serializer.GetNoteSerializationText(discount);

            // Assert
            Assert.NotEmpty(json);
            Assert.Contains("\"comment\":\"\"", json);
        }

        [Fact]
        public void GetNoteSerializationText_HandlesSpecialCharacters()
        {
            // Arrange
            var discount = CreateValidDiscount();
            discount.Program = "Test \"Program\" with <special> chars";
            discount.Comment = "Comment with & and \"quotes\"";

            // Act
            var json = _serializer.GetNoteSerializationText(discount);

            // Assert
            Assert.NotEmpty(json);
            var document = JsonDocument.Parse(json);
            Assert.NotNull(document);
        }

        [Fact]
        public void GetNoteSerializationText_SerializesMoneyAmount()
        {
            // Arrange
            var discount = CreateValidDiscount();
            discount.Amount = new Money(0.08m, CurrencyCodeType.Cad);

            // Act
            var json = _serializer.GetNoteSerializationText(discount);

            // Assert
            Assert.NotEmpty(json);
            Assert.Contains("\"amount\":", json);
        }

        [Fact]
        public void GetNoteSerializationText_IsActiveTrue()
        {
            // Arrange
            var discount = CreateValidDiscount();
            discount.IsActive = true;

            // Act
            var json = _serializer.GetNoteSerializationText(discount);

            // Assert
            Assert.Contains("\"isActive\":true", json);
        }

        [Fact]
        public void GetNoteSerializationText_IsActiveFalse()
        {
            // Arrange
            var discount = CreateValidDiscount();
            discount.IsActive = false;

            // Act
            var json = _serializer.GetNoteSerializationText(discount);

            // Assert
            Assert.Contains("\"isActive\":false", json);
        }

        #endregion

        #region GetEntity Tests

        [Fact]
        public async Task GetEntity_ValidNote_ReturnsDiscount()
        {
            // Arrange
            var discount = CreateValidDiscount();
            var json = _serializer.GetNoteSerializationText(discount);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(discount.Program, result.Value.Program);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesProgramField()
        {
            // Arrange
            var discount = CreateValidDiscount();
            discount.Program = "Petro-Canada Membership";

            var json = _serializer.GetNoteSerializationText(discount);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Petro-Canada Membership", result.Value.Program);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesAmountField()
        {
            // Arrange
            var discount = CreateValidDiscount();
            discount.Amount = new Money(0.05m, CurrencyCodeType.Cad);

            var json = _serializer.GetNoteSerializationText(discount);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value.Amount);
            Assert.Equal((double)0.05m, result.Value.Amount.InternalAmount);
            Assert.Equal(CurrencyCodeType.Cad, result.Value.Amount.Currency);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesDiscountTypeField()
        {
            // Arrange
            var discount = CreateValidDiscount();
            discount.DiscountType = GasDiscountType.Flat;

            var json = _serializer.GetNoteSerializationText(discount);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(GasDiscountType.Flat, result.Value.DiscountType);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesIsActiveField()
        {
            // Arrange
            var discount = CreateValidDiscount();
            discount.IsActive = false;

            var json = _serializer.GetNoteSerializationText(discount);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.False(result.Value.IsActive);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesCommentField()
        {
            // Arrange
            var discount = CreateValidDiscount();
            discount.Comment = "0.08 cents per liter discount";

            var json = _serializer.GetNoteSerializationText(discount);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("0.08 cents per liter discount", result.Value.Comment);
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
                Subject = AutomobileConstant.GasDiscountRecordSubject,
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
                Subject = AutomobileConstant.GasDiscountRecordSubject,
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
                Subject = AutomobileConstant.GasDiscountRecordSubject,
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
                Subject = AutomobileConstant.GasDiscountRecordSubject,
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
        public async Task GetEntity_MissingGasDiscountElement_ReturnsError()
        {
            // Arrange
            var note = new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = AutomobileConstant.GasDiscountRecordSubject,
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
            var discount = CreateValidDiscount();
            var json = _serializer.GetNoteSerializationText(discount);
            var note = new HmmNote
            {
                Id = 42,
                Author = _author,
                Subject = AutomobileConstant.GasDiscountRecordSubject,
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
            var discount = CreateValidDiscount();
            var json = _serializer.GetNoteSerializationText(discount);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(_author.Id, result.Value.AuthorId);
        }

        [Fact]
        public async Task GetEntity_DefaultsIsActiveToTrue()
        {
            // Arrange - create JSON without isActive field
            var json = "{\"note\":{\"content\":{\"GasDiscount\":{\"program\":\"Test\",\"discountType\":\"PerLiter\"}}}}";
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Value.IsActive);
        }

        #endregion

        #region GetNote Tests

        [Fact]
        public async Task GetNote_ValidEntity_ReturnsNote()
        {
            // Arrange
            var discount = CreateValidDiscount();

            // Act
            var result = await _serializer.GetNote(discount);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(AutomobileConstant.GasDiscountRecordSubject, result.Value.Subject);
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
            var discount = CreateValidDiscount(123);

            // Act
            var result = await _serializer.GetNote(discount);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(123, result.Value.Id);
        }

        [Fact]
        public async Task GetNote_SetsCorrectSubject()
        {
            // Arrange
            var discount = CreateValidDiscount();

            // Act
            var result = await _serializer.GetNote(discount);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(AutomobileConstant.GasDiscountRecordSubject, result.Value.Subject);
        }

        #endregion

        #region Round-Trip Tests

        [Fact]
        public async Task RoundTrip_AllFields_PreservesData()
        {
            // Arrange
            var original = CreateValidDiscount();

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(original.Program, result.Value.Program);
            Assert.Equal(original.DiscountType, result.Value.DiscountType);
            Assert.Equal(original.IsActive, result.Value.IsActive);
            Assert.Equal(original.Comment, result.Value.Comment);
        }

        [Fact]
        public async Task RoundTrip_MoneyAmount_PreservesData()
        {
            // Arrange
            var original = CreateValidDiscount();
            original.Amount = new Money(0.12m, CurrencyCodeType.Cad);

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value.Amount);
            Assert.Equal((double)0.12m, result.Value.Amount.InternalAmount);
            Assert.Equal(CurrencyCodeType.Cad, result.Value.Amount.Currency);
        }

        [Fact]
        public async Task RoundTrip_PerLiterDiscountType_PreservesData()
        {
            // Arrange
            var original = CreateValidDiscount();
            original.DiscountType = GasDiscountType.PerLiter;

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(GasDiscountType.PerLiter, result.Value.DiscountType);
        }

        [Fact]
        public async Task RoundTrip_FlatDiscountType_PreservesData()
        {
            // Arrange
            var original = CreateValidDiscount();
            original.DiscountType = GasDiscountType.Flat;

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(GasDiscountType.Flat, result.Value.DiscountType);
        }

        [Fact]
        public async Task RoundTrip_IsActiveFalse_PreservesData()
        {
            // Arrange
            var original = CreateValidDiscount();
            original.IsActive = false;

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.False(result.Value.IsActive);
        }

        [Fact]
        public async Task RoundTrip_SpecialCharacters_PreservesData()
        {
            // Arrange
            var original = CreateValidDiscount();
            original.Program = "Test \"Program\" <with> special & chars";
            original.Comment = "Special chars: \"quotes\" 'apostrophe' <tags> & ampersand";

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(original.Program, result.Value.Program);
            Assert.Equal(original.Comment, result.Value.Comment);
        }

        [Fact]
        public async Task RoundTrip_ZeroAmount_PreservesData()
        {
            // Arrange
            var original = CreateValidDiscount();
            original.Amount = new Money(0m, CurrencyCodeType.Cad);

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value.Amount);
            Assert.Equal((double)0m, result.Value.Amount.InternalAmount);
        }

        [Fact]
        public async Task RoundTrip_UsdCurrency_PreservesData()
        {
            // Arrange
            var original = CreateValidDiscount();
            original.Amount = new Money(0.10m, CurrencyCodeType.Usd);

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value.Amount);
            Assert.Equal(CurrencyCodeType.Usd, result.Value.Amount.Currency);
        }

        #endregion

        #region Helper Methods

        private GasDiscount CreateValidDiscount(int id = 0)
        {
            var discountId = id > 0 ? id : 1;
            return new GasDiscount
            {
                Id = discountId,
                AuthorId = _author.Id,
                Program = "Petro-Canada Membership",
                Amount = new Money(0.08m, CurrencyCodeType.Cad),
                DiscountType = GasDiscountType.PerLiter,
                IsActive = true,
                Comment = "0.08 cents per liter discount"
            };
        }

        private HmmNote CreateNote(string content)
        {
            return new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = AutomobileConstant.GasDiscountRecordSubject,
                Content = content,
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _serializer = new GasDiscountJsonNoteSerialize(Application, new NullLogger<GasDiscount>(), LookupRepository);
            _author = ApplicationRegister.DefaultAuthor;
        }

        #endregion
    }
}
